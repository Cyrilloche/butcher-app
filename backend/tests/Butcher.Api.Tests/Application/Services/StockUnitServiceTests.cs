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
        AppDbContext dbContext, SaleMode saleMode, string code = "SC", DateOnly? productionDate = null)
    {
        var product = new Product { Code = code, Name = "Saucisse curry", SaleMode = saleMode };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return await SeedBatchForProductAsync(dbContext, product, productionDate);
    }

    /// <summary>Une seconde fournée pour un produit déjà semé, la numérotation étant par produit.</summary>
    private static async Task<ProductionBatch> SeedBatchForProductAsync(
        AppDbContext dbContext, Product product, DateOnly? productionDate = null)
    {
        var batch = new ProductionBatch
        {
            ProductId = product.Id,
            Product = product,
            ProductionDate = productionDate ?? new DateOnly(2026, 8, 31),
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

        var result = await service.GetAllAsync(batch1.Id, status: null, productId: null);

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

    // Filtre par produit : alimente la sélection d'unités du solde, depuis la fiche produit.
    [Fact]
    public async Task GetAllAsync_FilteredByProductId_ReturnsUnitsOfEveryBatchOfThatProduct()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch1 = await SeedBatchAsync(dbContext, SaleMode.ByPiece, code: "SC");
        var batch2 = await SeedBatchAsync(dbContext, SaleMode.ByPiece, code: "JB");
        var service = new StockUnitService(dbContext);
        await service.AddUnitsAsync(batch1.Id, new AddStockUnitsRequest { Quantity = 2 });
        await service.AddUnitsAsync(batch2.Id, new AddStockUnitsRequest { Quantity = 3 });

        var result = await service.GetAllAsync(batchId: null, status: null, productId: batch1.ProductId);

        Assert.Equal(2, result.Count);
    }
    // --- Numérotation des unités -----------------------------------------------------------------
    //
    // Le numéro est ce que l'utilisateur recopie à la main sur l'étiquette. Ces tests sont les
    // gardiens de la seule propriété qui ne se rattrape pas : deux étiquettes identiques dans le
    // saloir sont indiscernables une fois écrites.

    [Fact]
    public async Task AddUnitsAsync_NumbersUnitsFromOneWithProductCodeAndProductionDate()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var service = new StockUnitService(dbContext);

        var result = await service.AddUnitsAsync(
            batch.Id, new AddStockUnitsRequest { Weights = [0.320m, 0.315m, 0.340m] });

        Assert.Equal(
            ["SC-260831-1", "SC-260831-2", "SC-260831-3"],
            result.Select(u => u.UnitNumber));
    }

    [Fact]
    public async Task AddUnitsAsync_SecondBatchOfTheSameDay_ContinuesTheSequence()
    {
        await using var dbContext = fixture.CreateDbContext();
        var morning = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var afternoon = await SeedBatchForProductAsync(dbContext, morning.Product!);
        var service = new StockUnitService(dbContext);

        await service.AddUnitsAsync(morning.Id, new AddStockUnitsRequest { Weights = [1m, 1m, 1m] });
        var result = await service.AddUnitsAsync(afternoon.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });

        Assert.Equal(["SC-260831-4", "SC-260831-5"], result.Select(u => u.UnitNumber));
    }

    [Fact]
    public async Task AddUnitsAsync_ForAnotherProductOnTheSameDay_RestartsAtOne()
    {
        await using var dbContext = fixture.CreateDbContext();
        var sausages = await SeedBatchAsync(dbContext, SaleMode.ByWeight, code: "SC");
        var hams = await SeedBatchAsync(dbContext, SaleMode.ByWeight, code: "JB");
        var service = new StockUnitService(dbContext);

        await service.AddUnitsAsync(sausages.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });
        var result = await service.AddUnitsAsync(hams.Id, new AddStockUnitsRequest { Weights = [1m] });

        Assert.Equal(["JB-260831-1"], result.Select(u => u.UnitNumber));
    }

    [Fact]
    public async Task AddUnitsAsync_OnAnotherDay_RestartsAtOne()
    {
        await using var dbContext = fixture.CreateDbContext();
        var first = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var next = await SeedBatchForProductAsync(dbContext, first.Product!, new DateOnly(2026, 9, 1));
        var service = new StockUnitService(dbContext);

        await service.AddUnitsAsync(first.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });
        var result = await service.AddUnitsAsync(next.Id, new AddStockUnitsRequest { Weights = [1m] });

        Assert.Equal(["SC-260901-1"], result.Select(u => u.UnitNumber));
    }

    // Une fournée saisie après coup porte la date de sa production, pas celle de la saisie : le
    // numéro écrit sur l'étiquette doit parler de la fabrication.
    [Fact]
    public async Task AddUnitsAsync_OnABatchProducedEarlier_UsesItsProductionDate()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(
            dbContext, SaleMode.ByWeight, productionDate: new DateOnly(2026, 7, 14));
        var service = new StockUnitService(dbContext);

        var result = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [1m] });

        Assert.Equal(["SC-260714-1"], result.Select(u => u.UnitNumber));
    }

    // Mécanisme de stock uniforme : une pièce est numérotée comme un sachet (CLAUDE.md §8 règle 2).
    [Fact]
    public async Task AddUnitsAsync_ByPiece_NumbersUnitsToo()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByPiece);
        var service = new StockUnitService(dbContext);

        var result = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Quantity = 2 });

        Assert.Equal(["SC-260831-1", "SC-260831-2"], result.Select(u => u.UnitNumber));
        Assert.All(result, u => Assert.Null(u.Weight));
    }

    // Deux pesées simultanées sur le même produit et le même jour. Sans le verrou de ligne, elles
    // calculent le même rang et l'une des deux échoue sur l'index unique.
    [Fact]
    public async Task AddUnitsAsync_ConcurrentCalls_ProduceDisjointNumbers()
    {
        await using var seedContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(seedContext, SaleMode.ByWeight);

        async Task<List<StockUnitDto>> AddThreeAsync()
        {
            await using var dbContext = fixture.CreateDbContext();
            var service = new StockUnitService(dbContext);
            return await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [1m, 1m, 1m] });
        }

        var results = await Task.WhenAll(AddThreeAsync(), AddThreeAsync());

        var numbers = results.SelectMany(r => r.Select(u => u.UnitNumber)).ToList();
        Assert.Equal(6, numbers.Count);
        Assert.Equal(6, numbers.Distinct().Count());
    }

    // Le rang d'une unité supprimée reste consommé : le reprendre ferait cohabiter deux étiquettes
    // manuscrites identiques, indiscernables une fois écrites.
    [Fact]
    public async Task DeleteAsync_ThenAddingAUnit_NeverReissuesTheFreedNumber()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batch = await SeedBatchAsync(dbContext, SaleMode.ByWeight);
        var service = new StockUnitService(dbContext);
        var created = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });

        await service.DeleteAsync(created[1].Id);
        var next = await service.AddUnitsAsync(batch.Id, new AddStockUnitsRequest { Weights = [1m] });

        Assert.Equal(["SC-260831-3"], next.Select(u => u.UnitNumber));
    }
}
