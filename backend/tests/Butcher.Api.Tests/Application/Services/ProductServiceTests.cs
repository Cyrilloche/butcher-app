using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class ProductServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Un produit se réduit à code / name / sale_mode / is_active : l'unité de vente a été retirée
    // du périmètre V1 (décision 2026-09-04), le mode de vente suffit à piloter l'affichage du prix.
    [Fact]
    public async Task CreateAsync_CreatesProduct()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "SC",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
        });

        Assert.True(result.Id > 0);
        Assert.Equal("SC", result.Code);
        Assert.Equal("Saucisse curry", result.Name);
        Assert.Equal(SaleMode.ByWeight, result.SaleMode);
        Assert.True(result.IsActive);
    }

    // Création possible sur une base entièrement vide : plus aucun référentiel à alimenter d'abord.
    [Fact]
    public async Task CreateAsync_OnEmptyDatabase_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "TR",
            Name = "Terrine",
            SaleMode = SaleMode.ByPiece,
        });

        Assert.True(result.Id > 0);
        Assert.Equal(SaleMode.ByPiece, result.SaleMode);
    }

    [Fact]
    public async Task CreateAsync_WithLowercaseCode_NormalizesToUppercase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "sc",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
        });

        Assert.Equal("SC", result.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Autre produit", SaleMode = SaleMode.ByWeight }));
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
        var service = new ProductService(dbContext);
        var active = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: false);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsAllProducts()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
    }

    // RG-01 + §4.1 : le mode de vente et le code sont définitifs (le code porte le numéro de lot) ;
    // seul le nom reste modifiable.
    [Fact]
    public async Task UpdateAsync_UpdatesName_ButNotCodeOrSaleMode()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Saucisse au curry" });

        Assert.Equal("Saucisse au curry", updated.Name);
        Assert.Equal("SC", updated.Code);
        Assert.Equal(SaleMode.ByWeight, updated.SaleMode);
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive_EvenWithExistingBatches()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });

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
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(created.Id);

        await service.ReactivateAsync(created.Id);

        var result = await service.GetByIdAsync(created.Id);
        Assert.True(result.IsActive);
    }
}
