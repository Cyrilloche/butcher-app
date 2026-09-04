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
            Type = movement.Type,
            Date = movement.Date,
            SoldWeight = movement.SoldWeight,
            Amount = movement.Amount,
            SaleId = movement.SaleId,
            SaleNumber = movement.Sale?.SaleNumber,
            CustomerId = movement.Sale?.CustomerId,
            CustomerName = FormatCustomerName(movement.Sale?.Customer),
            Notes = movement.Notes,
        };

    public static string? FormatCustomerName(Customer? customer) =>
        customer is null ? null : $"{customer.FirstName} {customer.LastName}".Trim();
}
