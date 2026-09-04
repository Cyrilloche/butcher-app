using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class ProductionBatchServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<Product> SeedProductAsync(AppDbContext dbContext, string code = "SC", bool isActive = true)
    {
        var product = new Product
        {
            Code = code,
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
            IsActive = isActive,
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    [Fact]
    public async Task CreateAsync_GeneratesBatchNumberWithExpectedFormat()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);
        var productionDate = new DateOnly(2026, 8, 31);

        var result = await service.CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = productionDate,
            SalePrice = 12.5m,
        });

        Assert.Equal("SC-260831-1", result.BatchNumber);
    }

    [Fact]
    public async Task CreateAsync_SameProductSameDay_IncrementsSequence()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);
        var productionDate = new DateOnly(2026, 8, 31);

        var first = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = productionDate, SalePrice = 12.5m });
        var second = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = productionDate, SalePrice = 12.5m });

        Assert.Equal("SC-260831-1", first.BatchNumber);
        Assert.Equal("SC-260831-2", second.BatchNumber);
    }

    [Fact]
    public async Task CreateAsync_DifferentDay_RestartsSequenceAtOne()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);

        var first = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = new DateOnly(2026, 8, 31), SalePrice = 12.5m });
        var second = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = new DateOnly(2026, 9, 1), SalePrice = 12.5m });

        Assert.Equal("SC-260831-1", first.BatchNumber);
        Assert.Equal("SC-260901-1", second.BatchNumber);
    }

    [Fact]
    public async Task CreateAsync_DifferentProductSameDay_RestartsSequenceAtOne()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product1 = await SeedProductAsync(dbContext, code: "SC");
        var product2 = await SeedProductAsync(dbContext, code: "JB");
        var service = new ProductionBatchService(dbContext);
        var productionDate = new DateOnly(2026, 8, 31);

        var first = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product1.Id, ProductionDate = productionDate, SalePrice = 12.5m });
        var second = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product2.Id, ProductionDate = productionDate, SalePrice = 8m });

        Assert.Equal("SC-260831-1", first.BatchNumber);
        Assert.Equal("JB-260831-1", second.BatchNumber);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownProduct_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductionBatchService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductionBatchRequest { ProductId = 999, ProductionDate = DateOnly.FromDateTime(DateTime.UtcNow), SalePrice = 12.5m }));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveProduct_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext, isActive: false);
        var service = new ProductionBatchService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = DateOnly.FromDateTime(DateTime.UtcNow), SalePrice = 12.5m }));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductionBatchService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_FilteredByProductId_ReturnsOnlyMatchingBatches()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product1 = await SeedProductAsync(dbContext, code: "SC");
        var product2 = await SeedProductAsync(dbContext, code: "JB");
        var service = new ProductionBatchService(dbContext);
        var productionDate = new DateOnly(2026, 8, 31);
        await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product1.Id, ProductionDate = productionDate, SalePrice = 12.5m });
        await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product2.Id, ProductionDate = productionDate, SalePrice = 8m });

        var result = await service.GetAllAsync(product1.Id);

        Assert.Single(result);
        Assert.Equal(product1.Id, result[0].ProductId);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMutableFields_ButNotProductOrDateOrBatchNumber()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);
        var created = await service.CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductionBatchRequest
        {
            SalePrice = 13m,
            RawMaterialRef = "Porc — grossiste X",
            ExpiryDate = new DateOnly(2026, 9, 30),
            Notes = "Cuisson plus longue",
        });

        Assert.Equal(13m, updated.SalePrice);
        Assert.Equal("Porc — grossiste X", updated.RawMaterialRef);
        Assert.Equal(new DateOnly(2026, 9, 30), updated.ExpiryDate);
        Assert.Equal("Cuisson plus longue", updated.Notes);
        Assert.Equal(created.BatchNumber, updated.BatchNumber);
        Assert.Equal(created.ProductionDate, updated.ProductionDate);
        Assert.Equal(created.ProductId, updated.ProductId);
    }
}
