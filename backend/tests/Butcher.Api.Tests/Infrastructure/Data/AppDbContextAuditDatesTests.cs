using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Infrastructure.Data;

[Collection(DatabaseCollection.Name)]
public class AppDbContextAuditDatesTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static Product NewProduct() => new()
    {
        Code = "SC",
        Name = "Saucisse curry",
        SaleMode = SaleMode.ByWeight,
    };

    [Fact]
    public async Task SaveChanges_NewEntity_StampsCreatedAtAndLeavesUpdatedAtEmpty()
    {
        var before = DateTimeOffset.UtcNow;
        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.Products.Add(NewProduct());
            await writeContext.SaveChangesAsync();
        }

        // Relu depuis la base : c'est la colonne qui doit porter la date, pas l'objet en mémoire.
        await using var readContext = fixture.CreateDbContext();
        var product = await readContext.Products.SingleAsync();
        Assert.InRange(product.CreatedAt, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_ModifiedEntity_StampsUpdatedAtAndKeepsCreatedAt()
    {
        await using var dbContext = fixture.CreateDbContext();
        var product = NewProduct();
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var createdAt = product.CreatedAt;

        product.Name = "Saucisse au curry";
        await dbContext.SaveChangesAsync();

        Assert.Equal(createdAt, product.CreatedAt);
        Assert.NotNull(product.UpdatedAt);
        Assert.True(product.UpdatedAt >= createdAt);
    }

    [Fact]
    public async Task SaveChanges_CreatedAtSetByCaller_IsKept()
    {
        var explicitDate = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        await using var dbContext = fixture.CreateDbContext();
        var customer = new Customer { LastName = "Client test", CreatedAt = explicitDate };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        Assert.Equal(explicitDate, customer.CreatedAt);
    }
}
