using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Infrastructure.Data;

/// <summary>
/// L'auteur des fabrications, ventes et sorties est posé à l'enregistrement, sans que les services
/// aient à le faire (RF-27, FR-020, ADR-011).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CreatedByStampingTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedAccountAsync(string email)
    {
        await using var dbContext = fixture.CreateDbContext();
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = email.Split('@')[0],
            Role = AccountRole.User,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    /// <summary>Crée produit, fabrication, unité, client, vente et ligne de vente, dans ce contexte.</summary>
    private static async Task<(ProductionBatch Batch, Sale Sale, StockMovement Movement)> SeedSaleChainAsync(
        AppDbContext dbContext)
    {
        var product = new Product { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight };
        var customer = new Customer { LastName = "Dupont", FirstName = "Jean" };
        dbContext.AddRange(product, customer);
        await dbContext.SaveChangesAsync();

        var batch = new ProductionBatch
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 9, 12),
            SalePrice = 12.5m,
        };
        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 0.320m };
        var sale = new Sale { SaleNumber = "V-260912-1", CustomerId = customer.Id, Date = DateTimeOffset.UtcNow };
        dbContext.AddRange(unit, sale);
        await dbContext.SaveChangesAsync();

        var movement = new StockMovement
        {
            StockUnitId = unit.Id,
            Type = MovementType.Sale,
            SaleId = sale.Id,
            Date = DateTimeOffset.UtcNow,
            SoldWeight = 0.320m,
            Amount = 4m,
        };
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        return (batch, sale, movement);
    }

    [Fact]
    public async Task SaveChanges_WithCurrentAccount_StampsAuthorOnBatchSaleAndMovement()
    {
        var accountId = await SeedAccountAsync("mireille@saloir.local");
        await using var dbContext = fixture.CreateDbContext(new FixedCurrentAccount(accountId));

        var (batch, sale, movement) = await SeedSaleChainAsync(dbContext);

        await using var reader = fixture.CreateDbContext();
        Assert.Equal(accountId, (await reader.ProductionBatches.SingleAsync(b => b.Id == batch.Id)).CreatedById);
        Assert.Equal(accountId, (await reader.Sales.SingleAsync(s => s.Id == sale.Id)).CreatedById);
        Assert.Equal(accountId, (await reader.StockMovements.SingleAsync(m => m.Id == movement.Id)).CreatedById);
    }

    [Fact]
    public async Task SaveChanges_WithoutCurrentAccount_LeavesAuthorUnknown()
    {
        await using var dbContext = fixture.CreateDbContext();

        var (batch, sale, movement) = await SeedSaleChainAsync(dbContext);

        Assert.Null(batch.CreatedById);
        Assert.Null(sale.CreatedById);
        Assert.Null(movement.CreatedById);
    }

    [Fact]
    public async Task SaveChanges_WithExplicitAuthor_KeepsIt()
    {
        var explicitAuthor = await SeedAccountAsync("jean@saloir.local");
        var currentAccount = await SeedAccountAsync("mireille@saloir.local");
        await using var dbContext = fixture.CreateDbContext(new FixedCurrentAccount(currentAccount));

        var customer = new Customer { LastName = "Dupont", FirstName = "Jean" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        var sale = new Sale
        {
            SaleNumber = "V-260912-1",
            CustomerId = customer.Id,
            Date = DateTimeOffset.UtcNow,
            CreatedById = explicitAuthor,
        };
        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();

        Assert.Equal(explicitAuthor, sale.CreatedById);
    }

    [Fact]
    public async Task SaveChanges_OnUpdate_DoesNotChangeAuthor()
    {
        var author = await SeedAccountAsync("jean@saloir.local");
        var otherAccount = await SeedAccountAsync("mireille@saloir.local");
        int saleId;
        await using (var authorContext = fixture.CreateDbContext(new FixedCurrentAccount(author)))
        {
            saleId = (await SeedSaleChainAsync(authorContext)).Sale.Id;
        }

        await using var otherContext = fixture.CreateDbContext(new FixedCurrentAccount(otherAccount));
        var sale = await otherContext.Sales.SingleAsync(s => s.Id == saleId);
        sale.Paid = true;
        await otherContext.SaveChangesAsync();

        Assert.Equal(author, sale.CreatedById);
    }
}
