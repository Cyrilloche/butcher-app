using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Butcher.Api.Application.Services;

public class ProductionBatchService(AppDbContext dbContext) : IProductionBatchService
{
    private const int MaxBatchNumberAttempts = 3;

    public async Task<List<ProductionBatchDto>> GetAllAsync(int? productId)
    {
        var query = dbContext.ProductionBatches.Include(b => b.Product).AsQueryable();

        if (productId is not null)
        {
            query = query.Where(b => b.ProductId == productId);
        }

        return await query
            .OrderByDescending(b => b.ProductionDate)
            .ThenByDescending(b => b.Id)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<ProductionBatchDto> GetByIdAsync(int id)
    {
        var batch = await FindOrThrowAsync(id);
        return ToDto(batch);
    }

    public async Task<ProductionBatchDto> CreateAsync(CreateProductionBatchRequest request)
    {
        var product = await FindActiveProductOrThrowAsync(request.ProductId);

        for (var attempt = 1; attempt <= MaxBatchNumberAttempts; attempt++)
        {
            var batchNumber = await GenerateBatchNumberAsync(product, request.ProductionDate);

            var batch = new ProductionBatch
            {
                BatchNumber = batchNumber,
                ProductId = product.Id,
                Product = product,
                ProductionDate = request.ProductionDate,
                SalePrice = request.SalePrice,
                RawMaterialRef = request.RawMaterialRef,
                ExpiryDate = request.ExpiryDate,
                Notes = request.Notes,
            };

            dbContext.ProductionBatches.Add(batch);

            try
            {
                await dbContext.SaveChangesAsync();
                return ToDto(batch);
            }
            catch (DbUpdateException exception) when (IsBatchNumberConflict(exception) && attempt < MaxBatchNumberAttempts)
            {
                dbContext.ProductionBatches.Remove(batch);
            }
        }

        throw new ConflictException("Impossible de générer un numéro de lot unique, réessayez.");
    }

    public async Task<ProductionBatchDto> UpdateAsync(int id, UpdateProductionBatchRequest request)
    {
        var batch = await FindOrThrowAsync(id);

        batch.SalePrice = request.SalePrice;
        batch.RawMaterialRef = request.RawMaterialRef;
        batch.ExpiryDate = request.ExpiryDate;
        batch.Notes = request.Notes;
        await dbContext.SaveChangesAsync();

        return ToDto(batch);
    }

    private async Task<ProductionBatch> FindOrThrowAsync(int id) =>
        await dbContext.ProductionBatches.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Lot de production {id} introuvable.");

    private async Task<Product> FindActiveProductOrThrowAsync(int productId)
    {
        var product = await dbContext.Products.FindAsync(productId)
            ?? throw new ConflictException($"Le produit {productId} n'existe pas.");

        if (!product.IsActive)
        {
            throw new ConflictException($"Le produit « {product.Name} » est désactivé et ne peut pas recevoir de nouveau lot.");
        }

        return product;
    }

    private async Task<string> GenerateBatchNumberAsync(Product product, DateOnly productionDate)
    {
        var existingCount = await dbContext.ProductionBatches
            .CountAsync(b => b.ProductId == product.Id && b.ProductionDate == productionDate);

        var sequence = existingCount + 1;
        return $"{product.Code}-{productionDate:yyMMdd}-{sequence}";
    }

    private static bool IsBatchNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ix_production_batch_batch_number" };

    private static ProductionBatchDto ToDto(ProductionBatch batch) =>
        new()
        {
            Id = batch.Id,
            BatchNumber = batch.BatchNumber,
            ProductId = batch.ProductId,
            ProductName = batch.Product?.Name ?? string.Empty,
            ProductionDate = batch.ProductionDate,
            SalePrice = batch.SalePrice,
            RawMaterialRef = batch.RawMaterialRef,
            ExpiryDate = batch.ExpiryDate,
            Notes = batch.Notes,
        };
}
