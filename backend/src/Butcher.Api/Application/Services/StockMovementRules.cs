using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Services;

/// <summary>
/// Règles communes aux mouvements de stock, partagées par <see cref="StockMovementService"/> (une
/// ligne à la fois) et <see cref="SaleService"/> (toutes les lignes d'une vente d'un coup).
/// </summary>
internal static class StockMovementRules
{
    public static void EnsureCanReceiveMovement(StockUnit unit)
    {
        if (unit.Status is not (StockUnitStatus.Available or StockUnitStatus.Opened))
        {
            throw new ConflictException(
                $"Cette unité est déjà finalisée (statut « {unit.Status} ») et ne peut plus recevoir de mouvement.");
        }
    }

    public static void ValidateSoldWeight(StockUnit unit, decimal? soldWeight)
    {
        var saleMode = unit.Batch!.Product!.SaleMode;

        if (saleMode == SaleMode.ByWeight)
        {
            if (soldWeight is null or <= 0)
            {
                throw new BadRequestException("« SoldWeight » est requis et doit être positif pour un produit vendu au poids.");
            }
        }
        else if (soldWeight is not null)
        {
            throw new BadRequestException("« SoldWeight » n'est pas applicable pour un produit vendu à la pièce.");
        }
    }

    public static void EnsurePartialSaleIsAllowed(StockUnit unit, MovementType type, bool isFullSale)
    {
        if (type != MovementType.Sale || isFullSale)
        {
            return;
        }

        if (!unit.Batch!.Product!.AllowPartialSale)
        {
            throw new ConflictException(
                $"Le produit « {unit.Batch.Product.Name} » n'autorise pas la vente à la tranche.");
        }
    }

    /// <summary>
    /// RG-05 : le poids restant d'une unité entamée n'est pas suivi, mais la somme des poids déjà
    /// vendus ne doit jamais dépasser le poids physique pesé de l'unité (<paramref name="existingSoldWeight"/>
    /// exclut le mouvement en cours d'écriture, le cas échéant).
    /// </summary>
    public static void EnsureSoldWeightWithinUnitCapacity(StockUnit unit, decimal existingSoldWeight, decimal? newSoldWeight)
    {
        if (newSoldWeight is null || unit.Weight is null)
        {
            return;
        }

        var total = existingSoldWeight + newSoldWeight.Value;
        if (total > unit.Weight.Value)
        {
            throw new ConflictException(
                $"La somme des poids vendus ({total:0.000} kg) dépasserait le poids de l'unité ({unit.Weight.Value:0.000} kg).");
        }
    }

    /// <summary>
    /// Poids encore vendable d'une unité : son poids pesé moins <paramref name="soldWeightSoFar"/>,
    /// la somme des poids déjà vendus sur elle (RG-05). Vaut le poids pesé sur une unité intacte,
    /// zéro sur une unité entièrement vendue, et <c>null</c> si l'unité n'a pas de poids — produit
    /// vendu à la pièce, ou unité au poids pas encore pesée.
    /// </summary>
    /// <remarks>
    /// Deux appelants, un seul calcul. <see cref="StockMovementService"/> et
    /// <see cref="ProductService"/> s'en servent pour le poids à **inscrire** sur une sortie perso
    /// ou perte : repartir du poids pesé y compterait deux fois la part déjà vendue. Le service des
    /// unités s'en sert pour le poids à **afficher** comme encore vendable. Le nom dit la valeur
    /// calculée, pas son premier usage.
    ///
    /// Un restant nul signifie que l'unité a été vendue en totalité. Côté sortie de stock, il n'y a
    /// alors plus rien à sortir et l'appelant doit la clôturer, plutôt qu'enregistrer une perte de
    /// poids nul que <see cref="ValidateSoldWeight"/> rejetterait. Côté affichage, c'est le signal
    /// qu'une clôture manuelle reste à faire (RG-04).
    ///
    /// Cette règle vit ici, et non dans le client : le principe II de la constitution fait du backend
    /// le garant des règles métier, et deux implémentations de celle-ci finiraient par diverger.
    /// Le poids ainsi calculé n'est jamais stocké — aucune colonne, aucun cache.
    /// </remarks>
    public static decimal? ComputeRemainingWeight(StockUnit unit, decimal soldWeightSoFar)
    {
        if (unit.Weight is null)
        {
            return null;
        }

        var remaining = unit.Weight.Value - soldWeightSoFar;
        return remaining > 0 ? remaining : 0m;
    }

    public static void ValidateAmount(MovementType type, decimal? amount)
    {
        if (type == MovementType.Sale)
        {
            if (amount is null or <= 0)
            {
                throw new BadRequestException("« Amount » est requis et doit être positif pour une vente.");
            }

            return;
        }

        if (amount is not null)
        {
            throw new BadRequestException("« Amount » n'est applicable que pour une vente.");
        }
    }

    public static StockUnitStatus DetermineNextStatus(StockUnitStatus currentStatus, MovementType type, bool isFullSale) =>
        (currentStatus, type) switch
        {
            (StockUnitStatus.Available, MovementType.Sale) => isFullSale ? StockUnitStatus.Sold : StockUnitStatus.Opened,
            (StockUnitStatus.Opened, MovementType.Sale) => StockUnitStatus.Opened,
            (_, MovementType.Personal) => StockUnitStatus.Personal,
            (_, MovementType.Loss) => StockUnitStatus.Lost,
            _ => currentStatus,
        };

    public static StockMovementDto ToDto(StockMovement movement) =>
        new()
        {
            Id = movement.Id,
            StockUnitId = movement.StockUnitId,
            ProductName = movement.StockUnit?.Batch?.Product?.Name,
            ProductIsActive = movement.StockUnit?.Batch?.Product?.IsActive ?? true,
            UnitNumber = movement.StockUnit?.UnitNumber,
            Type = movement.Type,
            Date = movement.Date,
            SoldWeight = movement.SoldWeight,
            Amount = movement.Amount,
            SaleId = movement.SaleId,
            SaleNumber = movement.Sale?.SaleNumber,
            CustomerId = movement.Sale?.CustomerId,
            CustomerName = FormatCustomerName(movement.Sale?.Customer),
            Notes = movement.Notes,
            CreatedByName = movement.CreatedBy?.DisplayName,
        };

    public static string? FormatCustomerName(Customer? customer) =>
        customer is null ? null : $"{customer.FirstName} {customer.LastName}".Trim();
}
