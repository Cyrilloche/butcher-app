using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class StockUnitService(AppDbContext dbContext) : IStockUnitService
{
    public async Task<List<StockUnitDto>> GetAllAsync(int? batchId, StockUnitStatus? status, int? productId)
    {
        var query = dbContext.StockUnits.Include(u => u.Batch).AsQueryable();

        if (batchId is not null)
        {
            query = query.Where(u => u.BatchId == batchId);
        }

        if (productId is not null)
        {
            query = query.Where(u => u.Batch!.ProductId == productId);
        }

        if (status is not null)
        {
            query = query.Where(u => u.Status == status);
        }

        // Le poids déjà vendu s'agrège en SQL, par sous-requête sur la navigation : une seule
        // requête part, quel que soit le nombre d'unités. Le calcul métier, lui, reste en C# —
        // la projection finale d'EF Core accepte un appel de méthode.
        var rows = await query
            .OrderBy(u => u.Id)
            .Select(u => new { Unit = u, SoldWeight = SoldWeightOf(u) })
            .ToListAsync();

        return rows.Select(row => ToDto(row.Unit, row.SoldWeight)).ToList();
    }

    public async Task<StockUnitDto> GetByIdAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);
        var soldWeight = await dbContext.StockMovements
            .Where(m => m.StockUnitId == id && m.Type == MovementType.Sale)
            .SumAsync(m => m.SoldWeight ?? 0m);

        return ToDto(unit, soldWeight);
    }

    /// <summary>
    /// Génère les unités physiques d'une fournée et leur attribue leur numéro d'étiquette.
    /// </summary>
    /// <remarks>
    /// C'est ici, et non à la création de la fournée, que les numéros sont émis : c'est le geste qui
    /// produit les objets à étiqueter. Une fournée enregistrée puis jamais pesée ne consomme donc
    /// aucun numéro. Les rangs sont réservés en une fois pour toute la demande, dans la transaction
    /// qui écrit les unités : si l'écriture échoue, aucun numéro n'est perdu.
    /// </remarks>
    public async Task<List<StockUnitDto>> AddUnitsAsync(int batchId, AddStockUnitsRequest request)
    {
        var batch = await dbContext.ProductionBatches.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new NotFoundException($"Lot de production {batchId} introuvable.");

        var weights = batch.Product!.SaleMode switch
        {
            SaleMode.ByWeight => ValidateWeightedRequest(request),
            SaleMode.ByPiece => ValidateCountedRequest(request),
            _ => throw new BadRequestException("Mode de vente du produit inconnu."),
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var firstSequence = await ReserveUnitNumbersAsync(batch, weights.Count);

        var units = weights
            .Select((weight, index) => new StockUnit
            {
                BatchId = batch.Id,
                Batch = batch,
                Weight = weight,
                UnitNumber = FormatUnitNumber(batch.Product!.Code, batch.ProductionDate, firstSequence + index),
            })
            .ToList();

        dbContext.StockUnits.AddRange(units);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        // Des unités qui viennent d'être pesées ne portent aucune vente : leur restant vaut leur poids.
        return units.Select(unit => ToDto(unit, 0m)).ToList();
    }

    /// <summary>
    /// Réserve <paramref name="count"/> rangs consécutifs pour le produit et la date de production de
    /// la fournée, et retourne le premier.
    /// </summary>
    /// <remarks>
    /// Deux gestes, tous deux à l'épreuve de la concurrence. La ligne du registre est d'abord créée
    /// si elle manque, en absorbant le conflit si quelqu'un vient de la créer ; elle est ensuite lue
    /// sous verrou de ligne, de sorte que deux générations simultanées sortent l'une après l'autre au
    /// lieu de calculer le même rang. Sans ce verrou, l'index unique sur le numéro d'unité
    /// rattraperait le coup, mais en renvoyant une erreur à l'utilisateur au lieu du numéro suivant.
    ///
    /// Le registre n'est jamais décrémenté ni purgé : c'est ce qui garantit qu'un numéro déjà écrit
    /// sur une étiquette ne soit jamais réémis (FR-004, FR-012).
    /// </remarks>
    private async Task<int> ReserveUnitNumbersAsync(ProductionBatch batch, int count)
    {
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO unit_number_sequence (product_id, production_date, last_sequence)
            VALUES ({batch.ProductId}, {batch.ProductionDate}, 0)
            ON CONFLICT (product_id, production_date) DO NOTHING
            """);

        // Pas d'opérateur LINQ après FromSql : la requête doit partir telle quelle, le verrou ne
        // survivrait pas à son encapsulation dans une sous-requête.
        var rows = await dbContext.UnitNumberSequences
            .FromSql(
                $"""
                SELECT product_id, production_date, last_sequence
                FROM unit_number_sequence
                WHERE product_id = {batch.ProductId} AND production_date = {batch.ProductionDate}
                FOR UPDATE
                """)
            .ToListAsync();

        var sequence = rows[0];
        var firstSequence = sequence.LastSequence + 1;
        sequence.LastSequence += count;

        return firstSequence;
    }

    /// <summary>
    /// Compose le numéro écrit sur l'étiquette : le code du produit, la date de production de la
    /// fournée, le rang. Trois segments, pas un de plus (FR-002, FR-015).
    /// </summary>
    private static string FormatUnitNumber(string productCode, DateOnly productionDate, int sequence) =>
        $"{productCode}-{productionDate:yyMMdd}-{sequence}";

    /// <summary>
    /// Supprime une unité disponible, pour corriger une erreur de pesée.
    /// </summary>
    /// <remarks>
    /// Le registre de numérotation n'est pas touché, et ne doit jamais l'être : le rang de l'unité
    /// supprimée reste consommé, faute de quoi la prochaine unité générée porterait un numéro déjà
    /// recopié sur une étiquette (FR-004).
    /// </remarks>
    public async Task DeleteAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);

        if (unit.Status != StockUnitStatus.Available)
        {
            throw new ConflictException("Seule une unité au statut « disponible » peut être supprimée.");
        }

        var hasMovements = await dbContext.StockMovements.AnyAsync(m => m.StockUnitId == id);
        if (hasMovements)
        {
            throw new ConflictException("Cette unité a déjà des mouvements de stock et ne peut pas être supprimée.");
        }

        dbContext.StockUnits.Remove(unit);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>Valide une demande au poids et retourne les poids pesés, dans l'ordre de saisie.</summary>
    private static List<decimal?> ValidateWeightedRequest(AddStockUnitsRequest request)
    {
        if (request.Quantity is not null)
        {
            throw new BadRequestException("« Quantity » n'est pas applicable pour un produit vendu au poids : fournir « Weights ».");
        }

        if (request.Weights is null || request.Weights.Count == 0)
        {
            throw new BadRequestException("Au moins un poids doit être fourni (« Weights »).");
        }

        if (request.Weights.Any(w => w <= 0))
        {
            throw new BadRequestException("Tous les poids doivent être strictement positifs.");
        }

        return request.Weights.Select(weight => (decimal?)weight).ToList();
    }

    /// <summary>
    /// Valide une demande à la pièce et retourne autant de poids nuls que de pièces : même
    /// mécanisme de stock que pour un produit au poids, numérotation comprise (CLAUDE.md §8 règle 2).
    /// </summary>
    private static List<decimal?> ValidateCountedRequest(AddStockUnitsRequest request)
    {
        if (request.Weights is not null)
        {
            throw new BadRequestException("« Weights » n'est pas applicable pour un produit vendu à la pièce : fournir « Quantity ».");
        }

        if (request.Quantity is null || request.Quantity <= 0)
        {
            throw new BadRequestException("« Quantity » doit être un nombre strictement positif.");
        }

        return Enumerable.Range(0, request.Quantity.Value).Select(_ => (decimal?)null).ToList();
    }

    private async Task<StockUnit> FindOrThrowAsync(int id) =>
        await dbContext.StockUnits.Include(u => u.Batch).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException($"Unité de stock {id} introuvable.");

    /// <summary>
    /// Somme des poids vendus sur une unité. Traduite en sous-requête corrélée par EF Core.
    /// </summary>
    /// <remarks>
    /// Seul le type vente est retranché. Une sortie perso ou une perte <b>finalise</b> l'unité :
    /// elle quitte le stock et n'est plus affichée, donc retrancher son poids ne servirait à rien
    /// et masquerait un filtre trop large derrière un restant faussement nul.
    /// </remarks>
    private static decimal SoldWeightOf(StockUnit unit) =>
        unit.StockMovements.Where(m => m.Type == MovementType.Sale).Sum(m => m.SoldWeight ?? 0m);

    private static StockUnitDto ToDto(StockUnit unit, decimal soldWeight) =>
        new()
        {
            Id = unit.Id,
            BatchId = unit.BatchId,
            UnitNumber = unit.UnitNumber,
            Weight = unit.Weight,
            RemainingWeight = StockMovementRules.ComputeRemainingWeight(unit, soldWeight),
            Status = unit.Status,
        };
}
