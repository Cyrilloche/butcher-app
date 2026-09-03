using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class StockMovementServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<StockUnit> SeedStockUnitAsync(
        AppDbContext dbContext, SaleMode saleMode, decimal? weight, string code = "SC")
    {
        var unitOfMeasure = new UnitOfMeasure { Label = $"kilogramme-{code}", Abbreviation = $"kg-{code}" };
        dbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await dbContext.SaveChangesAsync();

        var product = new Product { Code = code, Name = "Saucisse curry", SaleMode = saleMode, SaleUnitId = unitOfMeasure.Id };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var batch = new ProductionBatch
        {
            BatchNumber = $"{code}-260831-1",
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        };
        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        var stockUnit = new StockUnit { BatchId = batch.Id, Weight = weight };
        dbContext.StockUnits.Add(stockUnit);
        await dbContext.SaveChangesAsync();
        return stockUnit;
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext)
    {
        var customer = new Customer { LastName = "Dupont", FirstName = "Jean" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task CreateAsync_FullSaleByWeight_SetsUnitSoldAndStoresAmount()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = true,
            SoldWeight = 0.320m,
            Amount = 4.5m,
            CustomerId = customer.Id,
        });

        Assert.Equal(MovementType.Sale, result.Type);
        Assert.Equal(4.5m, result.Amount);
        Assert.Equal("Jean Dupont", result.CustomerName);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Sold, updatedUnit!.Status);
    }

    [Fact]
    public async Task CreateAsync_PartialSale_SetsUnitOpened_AndSubsequentSaleStaysOpened()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var service = new StockMovementService(dbContext);

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.150m,
            Amount = 2m,
        });

        var afterFirst = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, afterFirst!.Status);

        // isFullSale=true est ignoré une fois l'unité déjà "opened" : elle reste ouverte tant qu'elle
        // n'est pas clôturée manuellement (RG-04).
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = true,
            SoldWeight = 0.100m,
            Amount = 1.3m,
        });

        var afterSecond = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, afterSecond!.Status);
    }

    [Fact]
    public async Task CloseAsync_OnOpenedUnit_SetsSold()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m });

        await service.CloseAsync(unit.Id);

        var closedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Sold, closedUnit!.Status);
    }

    [Fact]
    public async Task CloseAsync_OnAvailableUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() => service.CloseAsync(unit.Id));
    }

    [Fact]
    public async Task CreateAsync_PersonalFromOpened_SetsPersonal()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m });

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.170m });

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Personal, updatedUnit!.Status);
    }

    [Fact]
    public async Task CreateAsync_OnAlreadyFinalizedUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, SoldWeight = 0.320m, Amount = 4m });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.1m }));
    }

    [Fact]
    public async Task CreateAsync_SaleWithoutAmount_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, SoldWeight = 0.320m }));
    }

    [Fact]
    public async Task CreateAsync_PersonalWithAmount_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.320m, Amount = 4m }));
    }

    [Fact]
    public async Task CreateAsync_ByPieceWithSoldWeight_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SoldWeight = 0.1m }));
    }

    [Fact]
    public async Task CreateAsync_SaleAnonymous_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m });

        Assert.Null(result.CustomerId);
        Assert.Null(result.CustomerName);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCustomer_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, CustomerId = 999 }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAmountAndNotes()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);
        var created = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m });

        var updated = await service.UpdateAsync(created.Id, new UpdateStockMovementRequest { Amount = 5.5m, Notes = "Remise fidélité" });

        Assert.Equal(5.5m, updated.Amount);
        Assert.Equal("Remise fidélité", updated.Notes);
    }

    [Fact]
    public async Task DeleteAsync_OnlyMovement_RevertsUnitToAvailable()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);
        var created = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m });

        await service.DeleteAsync(created.Id);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Available, updatedUnit!.Status);
    }

    [Fact]
    public async Task DeleteAsync_OneOfSeveralPartialMovements_KeepsUnitOpened()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var service = new StockMovementService(dbContext);
        var first = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m });
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.100m, Amount = 1.3m });

        await service.DeleteAsync(first.Id);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, updatedUnit!.Status);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByStockUnitId_ReturnsOnlyMatchingMovements()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit1 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "SC");
        var unit2 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "JB");
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit1.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m });
        await service.CreateAsync(unit2.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 6m });

        var result = await service.GetAllAsync(unit1.Id, customerId: null);

        Assert.Single(result);
        Assert.Equal(unit1.Id, result[0].StockUnitId);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }
}
