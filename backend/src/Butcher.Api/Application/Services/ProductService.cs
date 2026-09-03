using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<List<ProductDto>> GetAllAsync(bool includeInactive)
    {
        var query = dbContext.Products.Include(p => p.SaleUnit).AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query.Select(p => ToDto(p)).ToListAsync();
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await FindOrThrowAsync(id);
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        await EnsureCodeIsUniqueAsync(request.Code, excludingId: null);
        var saleUnit = await FindActiveSaleUnitOrThrowAsync(request.SaleUnitId);

        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            SaleMode = request.SaleMode,
            SaleUnitId = request.SaleUnitId,
            SaleUnit = saleUnit,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await FindOrThrowAsync(id);
        var saleUnit = await FindActiveSaleUnitOrThrowAsync(request.SaleUnitId);

        product.Name = request.Name;
        product.SaleUnitId = request.SaleUnitId;
        product.SaleUnit = saleUnit;
        await dbContext.SaveChangesAsync();

        return ToDto(product);
    }

    public async Task DeactivateAsync(int id)
    {
        var product = await FindOrThrowAsync(id);
        product.IsActive = false;
        await dbContext.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int id)
    {
        var product = await FindOrThrowAsync(id);
        product.IsActive = true;
        await dbContext.SaveChangesAsync();
    }

    private async Task<Product> FindOrThrowAsync(int id) =>
        await dbContext.Products.Include(p => p.SaleUnit).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Produit {id} introuvable.");

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId)
    {
        var exists = await dbContext.Products
            .Where(p => excludingId == null || p.Id != excludingId)
            .AnyAsync(p => p.Code == code);

        if (exists)
        {
            throw new ConflictException($"Un produit avec le code « {code} » existe déjà.");
        }
    }

    private async Task<UnitOfMeasure> FindActiveSaleUnitOrThrowAsync(int saleUnitId)
    {
        var unit = await dbContext.UnitsOfMeasure.FindAsync(saleUnitId)
            ?? throw new ConflictException($"L'unité de mesure {saleUnitId} n'existe pas.");

        if (!unit.IsActive)
        {
            throw new ConflictException($"L'unité de mesure « {unit.Label} » est désactivée et ne peut pas être utilisée.");
        }

        return unit;
    }

    private static ProductDto ToDto(Product product) =>
        new()
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            SaleMode = product.SaleMode,
            SaleUnitId = product.SaleUnitId,
            SaleUnitLabel = product.SaleUnit?.Label ?? string.Empty,
            IsActive = product.IsActive,
        };
}
