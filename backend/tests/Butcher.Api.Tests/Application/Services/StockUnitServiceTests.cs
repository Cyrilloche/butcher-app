using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class StockUnitServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<ProductionBatch> SeedBatchAsync(
        AppDbContext dbContext, SaleMode saleMode, string code = "SC")
    {
        var unit = new UnitOfMeasure { Label = $"kilogramme-{code}", Abbreviation = $"kg-{code}" };
        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();

        var product = new Product { Code = code, Name = "Saucisse curry", SaleMode = saleMode, SaleUnitId = unit.Id };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var batch = new ProductionBatch
        {
            BatchNumber = $"{code}-260831-1",
            ProductId = product.Id,
            Product = product,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        };
        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();
        return batch;
    }

    [Fact]
    public async Task AddUnitsAsync_ByWeight_CreatesOneUnitPerWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var service = new StockUnitService(dbContext);

        var result = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [0.320m, 0.315m, 0.340m] });

        Assert.Equal(3, result.Count);
        Assert.All(result, u => Assert.Equal(StockUnitStatus.Available, u.Status));
        Assert.Equal([0.320m, 0.315m, 0.340m], result.Select(u => u.Weight));
    }

    [Fact]
    public async Task AddUnitsAsync_ByWeight_WithQuantityProvided_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var service = new StockUnitService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Quantity = 5 }));
    }

    [Fact]
    public async Task AddUnitsAsync_ByWeight_WithNonPositiveWeight_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var service = new StockUnitService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [0.320m, 0m] }));
    }

    [Fact]
    public async Task AddUnitsAsync_ByPiece_CreatesQuantityUnitsWithNullWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByPiece);
        var service = new StockUnitService(dbContext);

        var result = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Quantity = 4 });

        Assert.Equal(4, result.Count);
        Assert.All(result, u => Assert.Null(u.Weight));
    }

    [Fact]
    public async Task AddUnitsAsync_ByPiece_WithWeightsProvided_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByPiece);
        var service = new StockUnitService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [0.320m] }));
    }

    [Fact]
    public async Task AddUnitsAsync_WithUnknownBatch_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new StockUnitService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddUnitsAsync(999, new AddStockUnitsRequest { Quantity = 1 }));
    }

    [Fact]
    public async Task GetAllAsync_FilteredByBatchId_ReturnsOnlyMatchingUnits()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch1 = await SeedBatchAsync(dbContext, SaleMode.ByPiece, code: "SC");
        var batch2 = await SeedBatchAsync(dbContext, SaleMode.ByPiece, code: "JB");
        var service = new StockUnitService(dbContext);
        await service.AddUnitsAsync(batch1.Id, new AddStockUnitsRequest { Quantity = 2 });
        await service.AddUnitsAsync(batch2.Id, new AddStockUnitsRequest { Quantity = 3 });

        var result = await service.GetAllAsync(batch1.Id, status: null);

        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.Equal(batch1.Id, u.BatchId));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new StockUnitService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_WhenAvailableAndUnused_RemovesUnit()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByPiece);
        var service = new StockUnitService(dbContext);
        var created = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Quantity = 1 });

        await service.DeleteAsync(created[0].Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created[0].Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAvailable_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByPiece);
        var service = new StockUnitService(dbContext);
        var created = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Quantity = 1 });

        var trackedUnit = await dbContext.StockUnits.FindAsync(created[0].Id);
        trackedUnit!.Status = StockUnitStatus.Sold;
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(created[0].Id));
    }
}
