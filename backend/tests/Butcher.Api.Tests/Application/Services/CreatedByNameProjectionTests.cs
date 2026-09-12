using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

/// <summary>
/// Le nom de l'auteur est exposé par les fabrications, les ventes et leurs lignes, et les sorties, dès
/// la réponse de création comme à la relecture (FR-020a, FR-026).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CreatedByNameProjectionTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<ICurrentAccount> SeedAccountAsync(string displayName)
    {
        await using var dbContext = fixture.CreateDbContext();
        var email = $"{displayName.ToLowerInvariant()}@saloir.local";
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            Role = AccountRole.User,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return new FixedCurrentAccount(account.Id);
    }

    /// <summary>Produit au poids, une fabrication et deux unités pesées, créés dans ce contexte.</summary>
    private static async Task<(int BatchId, int FirstUnitId, int SecondUnitId)> SeedStockAsync(
        AppDbContext dbContext, ProductionBatchService batchService)
    {
        var product = new Product { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var batch = await batchService.CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 9, 12),
            SalePrice = 12.5m,
        });

        var first = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 0.320m };
        var second = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 0.410m };
        dbContext.StockUnits.AddRange(first, second);
        await dbContext.SaveChangesAsync();

        return (batch.Id, first.Id, second.Id);
    }

    [Fact]
    public async Task Creations_ByNamedAccount_ExposeAuthorNameOnCreateAndRead()
    {
        var mireille = await SeedAccountAsync("Mireille");
        await using var dbContext = fixture.CreateDbContext(mireille);
        var batchService = new ProductionBatchService(dbContext);
        var saleService = new SaleService(dbContext);
        var movementService = new StockMovementService(dbContext);

        var (batchId, firstUnitId, secondUnitId) = await SeedStockAsync(dbContext, batchService);
        var customer = new Customer { LastName = "Dupont", FirstName = "Jean" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var createdSale = await saleService.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Paid = true,
            Lines = [new CreateSaleLineRequest { StockUnitId = firstUnitId, IsFullSale = true, SoldWeight = 0.320m, Amount = 4m }],
        });
        var createdOutcome = await movementService.CreateAsync(secondUnitId, new CreateStockMovementRequest
        {
            Type = MovementType.Personal,
            SoldWeight = 0.410m,
        });

        Assert.Equal("Mireille", createdSale.CreatedByName);
        Assert.Equal("Mireille", Assert.Single(createdSale.Lines).CreatedByName);
        Assert.Equal("Mireille", createdOutcome.CreatedByName);

        await using var reader = fixture.CreateDbContext();
        Assert.Equal("Mireille", (await new ProductionBatchService(reader).GetByIdAsync(batchId)).CreatedByName);
        var readSale = await new SaleService(reader).GetByIdAsync(createdSale.Id);
        Assert.Equal("Mireille", readSale.CreatedByName);
        Assert.Equal("Mireille", Assert.Single(readSale.Lines).CreatedByName);
        Assert.Equal("Mireille", (await new StockMovementService(reader).GetByIdAsync(createdOutcome.Id)).CreatedByName);
    }

    [Fact]
    public async Task Creations_WithoutAccount_ExposeNoAuthorName()
    {
        await using var dbContext = fixture.CreateDbContext();
        var batchService = new ProductionBatchService(dbContext);

        var (batchId, _, secondUnitId) = await SeedStockAsync(dbContext, batchService);
        var outcome = await new StockMovementService(dbContext).CreateAsync(secondUnitId, new CreateStockMovementRequest
        {
            Type = MovementType.Loss,
            SoldWeight = 0.410m,
        });

        Assert.Null((await batchService.GetByIdAsync(batchId)).CreatedByName);
        Assert.Null(outcome.CreatedByName);
    }
}
