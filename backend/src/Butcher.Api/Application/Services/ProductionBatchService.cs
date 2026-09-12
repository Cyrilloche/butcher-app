using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class ProductionBatchService(AppDbContext dbContext) : IProductionBatchService
{
    public async Task<List<ProductionBatchDto>> GetAllAsync(int? productId)
    {
        var query = dbContext.ProductionBatches.Include(b => b.Product).Include(b => b.CreatedBy).AsQueryable();

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

        // Une fabrication ne porte plus de numéro : c'est l'unité physique qui en porte un, attribué
        // au moment où elle est générée (voir StockUnitService). Enregistrer une fournée est donc
        // redevenu une écriture simple, sans registre ni transaction propre.
        var batch = new ProductionBatch
        {
            ProductId = product.Id,
            Product = product,
            ProductionDate = request.ProductionDate,
            SalePrice = request.SalePrice,
            RawMaterialRef = request.RawMaterialRef,
            ExpiryDate = request.ExpiryDate,
            Notes = request.Notes,
        };

        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        // SaveChanges n'a posé que l'identifiant de l'auteur : on charge le compte pour exposer son nom.
        await dbContext.Entry(batch).Reference(b => b.CreatedBy).LoadAsync();

        return ToDto(batch);
    }

    /// <summary>
    /// Supprime un lot et les unités de stock qu'il a générées, tant qu'aucune de ces unités n'a fait
    /// l'objet d'une sortie (FR-010 à FR-012).
    /// </summary>
    /// <remarks>
    /// Les unités sont supprimées explicitement, dans la même transaction, plutôt que par une cascade
    /// déclarée en base : le <c>Restrict</c> de <c>StockUnitConfiguration</c> reste en filet, de sorte
    /// qu'un contournement de cette vérification ferait échouer l'écriture au lieu de détruire de
    /// l'historique. Les numéros des unités supprimées ne sont pas libérés : le registre de séquences
/// n'est pas touché ici, et ne doit jamais l'être (FR-004).
    /// </remarks>
    public async Task DeleteAsync(int id)
    {
        var batch = await dbContext.ProductionBatches
            .Include(b => b.StockUnits)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Lot de production {id} introuvable.");

        var unitIds = batch.StockUnits.Select(u => u.Id).ToList();

        var unitsWithMovements = await dbContext.StockMovements
            .Where(m => unitIds.Contains(m.StockUnitId))
            .Select(m => m.StockUnitId)
            .Distinct()
            .CountAsync();

        if (unitsWithMovements > 0)
        {
            throw new ConflictException(
                $"Ce lot ne peut plus être supprimé : {unitsWithMovements} "
                + (unitsWithMovements > 1 ? "unités sont déjà sorties du stock" : "unité est déjà sortie du stock")
                + " (vente, perso ou perte).");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        dbContext.StockUnits.RemoveRange(batch.StockUnits);
        dbContext.ProductionBatches.Remove(batch);
        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
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
        await dbContext.ProductionBatches.Include(b => b.Product).Include(b => b.CreatedBy).FirstOrDefaultAsync(b => b.Id == id)
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

    private static ProductionBatchDto ToDto(ProductionBatch batch) =>
        new()
        {
            Id = batch.Id,
            ProductId = batch.ProductId,
            ProductName = batch.Product?.Name ?? string.Empty,
            ProductionDate = batch.ProductionDate,
            SalePrice = batch.SalePrice,
            RawMaterialRef = batch.RawMaterialRef,
            ExpiryDate = batch.ExpiryDate,
            Notes = batch.Notes,
            CreatedByName = batch.CreatedBy?.DisplayName,
        };
}
