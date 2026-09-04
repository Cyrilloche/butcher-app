using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class StockMovementService(AppDbContext dbContext) : IStockMovementService
{
    public async Task<List<StockMovementDto>> GetAllAsync(int? stockUnitId, int? customerId, int? saleId)
    {
        var query = BaseQuery();

        if (stockUnitId is not null)
        {
            query = query.Where(m => m.StockUnitId == stockUnitId);
        }

        if (customerId is not null)
        {
            query = query.Where(m => m.Sale != null && m.Sale.CustomerId == customerId);
        }

        if (saleId is not null)
        {
            query = query.Where(m => m.SaleId == saleId);
        }

        var movements = await query.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id).ToListAsync();
        return movements.Select(StockMovementRules.ToDto).ToList();
    }

    public async Task<StockMovementDto> GetByIdAsync(int id)
    {
        var movement = await FindOrThrowAsync(id);
        return StockMovementRules.ToDto(movement);
    }

    public async Task<StockMovementDto> CreateAsync(int stockUnitId, CreateStockMovementRequest request)
    {
        var unit = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .FirstOrDefaultAsync(u => u.Id == stockUnitId)
            ?? throw new NotFoundException($"Unité de stock {stockUnitId} introuvable.");

        StockMovementRules.EnsureCanReceiveMovement(unit);
        StockMovementRules.ValidateSoldWeight(unit, request.SoldWeight);
        StockMovementRules.ValidateAmount(request.Type, request.Amount);
        var sale = await ValidateAndResolveSaleAsync(request.Type, request.SaleId);

        var movement = new StockMovement
        {
            StockUnitId = unit.Id,
            StockUnit = unit,
            Type = request.Type,
            Date = sale?.Date ?? DateTimeOffset.UtcNow,
            SoldWeight = request.SoldWeight,
            Amount = request.Amount,
            SaleId = sale?.Id,
            Sale = sale,
            Notes = request.Notes,
        };

        unit.Status = StockMovementRules.DetermineNextStatus(unit.Status, request.Type, request.IsFullSale);

        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        return StockMovementRules.ToDto(movement);
    }

    public async Task<StockMovementDto> UpdateAsync(int id, UpdateStockMovementRequest request)
    {
        var movement = await FindOrThrowAsync(id);
        var unit = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .FirstAsync(u => u.Id == movement.StockUnitId);

        StockMovementRules.ValidateSoldWeight(unit, request.SoldWeight);
        StockMovementRules.ValidateAmount(movement.Type, request.Amount);

        movement.SoldWeight = request.SoldWeight;
        movement.Amount = request.Amount;
        movement.Notes = request.Notes;
        await dbContext.SaveChangesAsync();

        return StockMovementRules.ToDto(movement);
    }

    public async Task DeleteAsync(int id)
    {
        var movement = await FindOrThrowAsync(id);
        var unit = await dbContext.StockUnits.FirstAsync(u => u.Id == movement.StockUnitId);

        // Une vente ne peut pas se retrouver sans ligne : supprimer sa dernière ligne, c'est
        // supprimer la vente (passer par DELETE /api/sales/{id}, qui est explicite).
        if (movement.SaleId is not null)
        {
            var siblingLines = await dbContext.StockMovements
                .CountAsync(m => m.SaleId == movement.SaleId && m.Id != id);

            if (siblingLines == 0)
            {
                throw new ConflictException(
                    "C'est la dernière ligne de la vente : supprimez la vente elle-même plutôt que cette ligne.");
            }
        }

        dbContext.StockMovements.Remove(movement);

        var remainingMovements = await dbContext.StockMovements
            .CountAsync(m => m.StockUnitId == unit.Id && m.Id != id);

        // Règle volontairement simple : si plus aucun mouvement ne subsiste sur l'unité, elle redevient
        // disponible (annule une saisie erronée). Dans le cas contraire (ex. une vente partielle parmi
        // d'autres), le statut n'est pas recalculé plus finement — une clôture déjà faite ne "rouvre" pas.
        if (remainingMovements == 0)
        {
            unit.Status = StockUnitStatus.Available;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task CloseAsync(int stockUnitId)
    {
        var unit = await dbContext.StockUnits.FindAsync(stockUnitId)
            ?? throw new NotFoundException($"Unité de stock {stockUnitId} introuvable.");

        if (unit.Status != StockUnitStatus.Opened)
        {
            throw new ConflictException($"Seule une unité au statut « opened » peut être clôturée (statut actuel : « {unit.Status} »).");
        }

        unit.Status = StockUnitStatus.Sold;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Une vente est toujours rattachée à une <see cref="Sale"/>, qui porte le client obligatoire
    /// (RF-17 / RG-07) ; à l'inverse, un mouvement « perso » ou « perte » n'en a jamais.
    /// </summary>
    private async Task<Sale?> ValidateAndResolveSaleAsync(MovementType type, int? saleId)
    {
        if (type == MovementType.Sale)
        {
            if (saleId is null)
            {
                throw new BadRequestException(
                    "« SaleId » est requis pour une vente : créez la vente (POST /api/sales) puis rattachez-y la ligne.");
            }

            return await dbContext.Sales.Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == saleId)
                ?? throw new ConflictException($"La vente {saleId} n'existe pas.");
        }

        if (saleId is not null)
        {
            throw new BadRequestException("« SaleId » n'est applicable que pour une vente.");
        }

        return null;
    }

    private IQueryable<StockMovement> BaseQuery() =>
        dbContext.StockMovements
            .Include(m => m.Sale!).ThenInclude(s => s.Customer)
            .Include(m => m.StockUnit!).ThenInclude(u => u.Batch!).ThenInclude(b => b.Product);

    private async Task<StockMovement> FindOrThrowAsync(int id) =>
        await BaseQuery().FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Mouvement de stock {id} introuvable.");
}
