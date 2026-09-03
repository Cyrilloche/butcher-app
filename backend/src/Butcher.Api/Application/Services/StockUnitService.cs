using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class StockUnitService(AppDbContext dbContext) : IStockUnitService
{
    public async Task<List<StockUnitDto>> GetAllAsync(int? batchId, StockUnitStatus? status)
    {
        var query = dbContext.StockUnits.Include(u => u.Batch).AsQueryable();

        if (batchId is not null)
        {
            query = query.Where(u => u.BatchId == batchId);
        }

        if (status is not null)
        {
            query = query.Where(u => u.Status == status);
        }

        return await query.OrderBy(u => u.Id).Select(u => ToDto(u)).ToListAsync();
    }

    public async Task<StockUnitDto> GetByIdAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);
        return ToDto(unit);
    }

    public async Task<List<StockUnitDto>> AddUnitsAsync(int batchId, AddStockUnitsRequest request)
    {
        var batch = await dbContext.ProductionBatches.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new NotFoundException($"Lot de production {batchId} introuvable.");

        var units = batch.Product!.SaleMode switch
        {
            SaleMode.ByWeight => BuildWeightedUnits(batch, request),
            SaleMode.ByPiece => BuildCountedUnits(batch, request),
            _ => throw new BadRequestException("Mode de vente du produit inconnu."),
        };

        dbContext.StockUnits.AddRange(units);
        await dbContext.SaveChangesAsync();

        return units.Select(ToDto).ToList();
    }

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

    private static List<StockUnit> BuildWeightedUnits(ProductionBatch batch, AddStockUnitsRequest request)
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

        return request.Weights
            .Select(weight => new StockUnit { BatchId = batch.Id, Batch = batch, Weight = weight })
            .ToList();
    }

    private static List<StockUnit> BuildCountedUnits(ProductionBatch batch, AddStockUnitsRequest request)
    {
        if (request.Weights is not null)
        {
            throw new BadRequestException("« Weights » n'est pas applicable pour un produit vendu à la pièce : fournir « Quantity ».");
        }

        if (request.Quantity is null || request.Quantity <= 0)
        {
            throw new BadRequestException("« Quantity » doit être un nombre strictement positif.");
        }

        return Enumerable.Range(0, request.Quantity.Value)
            .Select(_ => new StockUnit { BatchId = batch.Id, Batch = batch, Weight = null })
            .ToList();
    }

    private async Task<StockUnit> FindOrThrowAsync(int id) =>
        await dbContext.StockUnits.Include(u => u.Batch).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException($"Unité de stock {id} introuvable.");

    private static StockUnitDto ToDto(StockUnit unit) =>
        new()
        {
            Id = unit.Id,
            BatchId = unit.BatchId,
            BatchNumber = unit.Batch?.BatchNumber ?? string.Empty,
            Weight = unit.Weight,
            Status = unit.Status,
        };
}
