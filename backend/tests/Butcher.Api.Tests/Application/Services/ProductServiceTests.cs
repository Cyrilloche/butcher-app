using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class ProductServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<UnitOfMeasure> SeedUnitOfMeasureAsync(
        AppDbContext dbContext, bool isActive = true, string label = "kilogramme", string abbreviation = "kg")
    {
        var unit = new UnitOfMeasure { Label = label, Abbreviation = abbreviation, IsActive = isActive };
        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit;
    }

    [Fact]
    public async Task CreateAsync_WithActiveSaleUnit_CreatesProduct()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "SC",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
            SaleUnitId = unit.Id,
        });

        Assert.True(result.Id > 0);
        Assert.Equal("SC", result.Code);
        Assert.Equal(SaleMode.ByWeight, result.SaleMode);
        Assert.Equal("kilogramme", result.SaleUnitLabel);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Autre produit", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id }));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSaleUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = 999 }));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveSaleUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext, isActive: false);
        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id }));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ByDefault_ReturnsOnlyActiveProducts()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);
        var active = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: false);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsAllProducts()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndSaleUnit_ButNotCodeOrSaleMode()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var otherUnit = new UnitOfMeasure { Label = "piece", Abbreviation = "pc", IsActive = true };
        dbContext.UnitsOfMeasure.Add(otherUnit);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Saucisse au curry", SaleUnitId = otherUnit.Id });

        Assert.Equal("Saucisse au curry", updated.Name);
        Assert.Equal(otherUnit.Id, updated.SaleUnitId);
        Assert.Equal("SC", updated.Code);
        Assert.Equal(SaleMode.ByWeight, updated.SaleMode);
    }

    [Fact]
    public async Task UpdateAsync_WithInactiveSaleUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var inactiveUnit = await SeedUnitOfMeasureAsync(dbContext, isActive: false, label: "piece", abbreviation: "pc");
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Saucisse curry", SaleUnitId = inactiveUnit.Id }));
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive_EvenWithExistingBatches()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });

        dbContext.ProductionBatches.Add(new ProductionBatch
        {
            BatchNumber = "SC-260101-1",
            ProductId = created.Id,
            ProductionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SalePrice = 12.5m,
        });
        await dbContext.SaveChangesAsync();

        await service.DeactivateAsync(created.Id);

        var result = await service.GetByIdAsync(created.Id);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_SetsActive()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedUnitOfMeasureAsync(dbContext);
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight, SaleUnitId = unit.Id });
        await service.DeactivateAsync(created.Id);

        await service.ReactivateAsync(created.Id);

        var result = await service.GetByIdAsync(created.Id);
        Assert.True(result.IsActive);
    }
}
