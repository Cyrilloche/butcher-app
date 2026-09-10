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
    public async Task UpdateAsync_UpdatesMutableFields_ButNotProductOrDate()
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
        Assert.Equal(created.ProductionDate, updated.ProductionDate);
        Assert.Equal(created.ProductId, updated.ProductId);
    }

    // Le point le plus exposé aux régressions : un comptage des lots existants réémettrait « 2 »
    // sur une seconde série d'étiquettes manuscrites, indiscernable de la première.
    // Une fabrication ne porte plus de numéro : c'est l'unité physique qui en porte un (FR-007).
    // Le test le dit du seul endroit où il peut le dire, la forme de la ressource renvoyée.
    [Fact]
    public async Task CreateAsync_ReturnsABatchWithoutAnyNumberOfItsOwn()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);

        var result = await service.CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        });

        Assert.DoesNotContain(
            typeof(ProductionBatchDto).GetProperties(),
            property => property.Name.Contains("Number", StringComparison.Ordinal));
        Assert.Equal(product.Id, result.ProductId);
    }

    // --- Suppression d'un lot (FR-010 à FR-012, SC-003) -----------------------------------------

    [Fact]
    public async Task DeleteAsync_OnIntactBatch_RemovesBatchAndItsStockUnits()
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
        dbContext.StockUnits.AddRange(
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = created.Id, Weight = 1.2m },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = created.Id, Weight = 1.4m });
        await dbContext.SaveChangesAsync();

        await service.DeleteAsync(created.Id);

        Assert.Empty(dbContext.ProductionBatches.Where(b => b.Id == created.Id));
        Assert.Empty(dbContext.StockUnits.Where(u => u.BatchId == created.Id));
    }

    [Fact]
    public async Task DeleteAsync_OnBatchWithoutUnits_Succeeds()
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

        await service.DeleteAsync(created.Id);

        Assert.Empty(dbContext.ProductionBatches.Where(b => b.Id == created.Id));
    }

    [Theory]
    [InlineData(MovementType.Sale)]
    [InlineData(MovementType.Personal)]
    [InlineData(MovementType.Loss)]
    public async Task DeleteAsync_OnBatchWithAnyOutcome_ThrowsConflictException(MovementType type)
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
        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = created.Id, Weight = 1.2m };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        dbContext.StockMovements.Add(new StockMovement
        {
            StockUnitId = unit.Id,
            Type = type,
            Date = DateTimeOffset.UtcNow,
            SoldWeight = 1.2m,
            Amount = type == MovementType.Sale ? 15m : null,
            SaleId = null,
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(created.Id));

        Assert.NotEmpty(dbContext.ProductionBatches.Where(b => b.Id == created.Id));
        Assert.NotEmpty(dbContext.StockUnits.Where(u => u.Id == unit.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductionBatchService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }

    // La soupape qui rend le gel du produit vivable : sans dernier lot, le produit redevient
    // entièrement modifiable (FR-015).
    [Fact]
    public async Task DeleteAsync_OfTheLastBatch_MakesProductNeverUsedAgain()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var batchService = new ProductionBatchService(dbContext);
        var productService = new ProductService(dbContext);
        var created = await batchService.CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        });
        Assert.True((await productService.GetByIdAsync(product.Id)).IsUsed);

        await batchService.DeleteAsync(created.Id);

        Assert.False((await productService.GetByIdAsync(product.Id)).IsUsed);
    }

    [Fact]
    public async Task DeleteAsync_WhenAnotherBatchRemains_LeavesProductUsed()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var batchService = new ProductionBatchService(dbContext);
        var productService = new ProductService(dbContext);
        var day = new DateOnly(2026, 8, 31);
        var first = await batchService.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = day, SalePrice = 12.5m });
        await batchService.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = day, SalePrice = 12.5m });

        await batchService.DeleteAsync(first.Id);

        Assert.True((await productService.GetByIdAsync(product.Id)).IsUsed);
    }
    // --- La suppression d'une fournée ne libère aucun numéro (FR-004, FR-010, FR-012) -------------

    // Le point le plus exposé aux régressions. Un comptage des unités existantes réémettrait les
    // rangs de la fournée supprimée sur une seconde série d'étiquettes manuscrites.
    [Fact]
    public async Task DeleteAsync_ThenCreatingABatchTheSameDay_NeverReissuesTheFreedNumbers()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);
        var units = new StockUnitService(dbContext);
        var day = new DateOnly(2026, 8, 31);
        var morning = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = day, SalePrice = 12.5m });
        await units.AddUnitsAsync(morning.Id, new AddStockUnitsRequest { Weights = [1m, 1m, 1m] });

        await service.DeleteAsync(morning.Id);
        var afternoon = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = day, SalePrice = 13m });
        var result = await units.AddUnitsAsync(afternoon.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });

        Assert.Equal(["SC-260831-4", "SC-260831-5"], result.Select(u => u.UnitNumber));
    }

    // La ligne du registre est ce qui porte la mémoire des rangs émis : elle doit survivre à la
    // disparition de toutes les fournées du jour.
    [Fact]
    public async Task DeleteAsync_OnTheOnlyBatchOfTheDay_KeepsItsNumberingRegistryRow()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new ProductionBatchService(dbContext);
        var units = new StockUnitService(dbContext);
        var day = new DateOnly(2026, 8, 31);
        var only = await service.CreateAsync(new CreateProductionBatchRequest { ProductId = product.Id, ProductionDate = day, SalePrice = 12.5m });
        await units.AddUnitsAsync(only.Id, new AddStockUnitsRequest { Weights = [1m, 1m] });

        await service.DeleteAsync(only.Id);

        var sequence = await dbContext.UnitNumberSequences.FindAsync(product.Id, day);
        Assert.NotNull(sequence);
        Assert.Equal(2, sequence.LastSequence);
    }
}
