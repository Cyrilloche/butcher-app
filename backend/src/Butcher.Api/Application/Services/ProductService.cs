using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<List<ProductDto>> GetAllAsync(bool includeInactive)
    {
        var query = dbContext.Products.AsQueryable();

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
        var code = request.Code.ToUpperInvariant();
        await EnsureCodeIsUniqueAsync(code, excludingId: null);
        EnsureAllowPartialSaleIsApplicable(request.SaleMode, request.AllowPartialSale);

        var product = new Product
        {
            Code = code,
            Name = request.Name,
            SaleMode = request.SaleMode,
            AllowPartialSale = request.AllowPartialSale,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await FindOrThrowAsync(id);
        EnsureAllowPartialSaleIsApplicable(product.SaleMode, request.AllowPartialSale);

        product.Name = request.Name;
        product.AllowPartialSale = request.AllowPartialSale;
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
        await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id)
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

    private static void EnsureAllowPartialSaleIsApplicable(SaleMode saleMode, bool allowPartialSale)
    {
        if (allowPartialSale && saleMode != SaleMode.ByWeight)
        {
            throw new BadRequestException(
                "« AllowPartialSale » n'est applicable qu'aux produits vendus au poids.");
        }
    }

    private static ProductDto ToDto(Product product) =>
        new()
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            SaleMode = product.SaleMode,
            AllowPartialSale = product.AllowPartialSale,
            IsActive = product.IsActive,
        };
}
