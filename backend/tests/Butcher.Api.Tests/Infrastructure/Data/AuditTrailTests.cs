using System.Text.Json;
using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Infrastructure.Data;

/// <summary>
/// Le journal raconte des gestes, pas des lignes de base : une entrée par geste, contenu gardé pour
/// les suppressions, effets induits ignorés (FR-021, FR-022 ; clarifications du 2026-09-13).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AuditTrailTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private const string UserPassword = "Jambon-Saloir-2026-Mamie";
    private const string AdminPassword = "Correct-Horse4-Battery-Staple-Saloir";
    private static readonly DateOnly ProductionDate = new(2026, 9, 13);

    private Guid _accountId;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();

        await using var dbContext = fixture.CreateDbContext();
        var account = new AppUser
        {
            UserName = "mireille@saloir.local",
            NormalizedUserName = "MIREILLE@SALOIR.LOCAL",
            Email = "mireille@saloir.local",
            NormalizedEmail = "MIREILLE@SALOIR.LOCAL",
            DisplayName = "Mireille",
            Role = AccountRole.Admin,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        _accountId = account.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext CreateContext() => fixture.CreateDbContext(new FixedCurrentAccount(_accountId));

    private async Task<long> MarkAsync()
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.AuditEntries.MaxAsync(e => (long?)e.Id) ?? 0;
    }

    private async Task<List<AuditEntry>> EntriesAfterAsync(long marker)
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.AuditEntries.Where(e => e.Id > marker).OrderBy(e => e.Id).ToListAsync();
    }

    private record Stock(ProductDto Product, ProductionBatchDto Batch, List<StockUnitDto> Units, CustomerDto Customer);

    /// <summary>Jambon sec vendable à la tranche, une fournée de trois unités pesées, un client.</summary>
    private static async Task<Stock> SeedStockAsync(AppDbContext dbContext)
    {
        var product = await new ProductService(dbContext).CreateAsync(new CreateProductRequest
        {
            Code = "JB",
            Name = "Jambon sec",
            SaleMode = SaleMode.ByWeight,
            AllowPartialSale = true,
        });
        var batch = await new ProductionBatchService(dbContext).CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = product.Id,
            ProductionDate = ProductionDate,
            SalePrice = 20m,
        });
        var units = await new StockUnitService(dbContext).AddUnitsAsync(
            batch.Id, new AddStockUnitsRequest { Weights = [1.000m, 0.500m, 0.400m] });
        var customer = await new CustomerService(dbContext).CreateAsync(
            new CreateCustomerRequest { LastName = "Dupont", FirstName = "Jean" });

        return new Stock(product, batch, units, customer);
    }

    private static Task<SaleDto> SellTwoUnitsAsync(AppDbContext dbContext, Stock stock) =>
        new SaleService(dbContext).CreateAsync(new CreateSaleRequest
        {
            CustomerId = stock.Customer.Id,
            Paid = true,
            Lines =
            [
                new CreateSaleLineRequest { StockUnitId = stock.Units[0].Id, SoldWeight = 1.000m, Amount = 20m },
                new CreateSaleLineRequest { StockUnitId = stock.Units[1].Id, SoldWeight = 0.500m, Amount = 10m },
            ],
        });

    [Fact]
    public async Task CreatingASale_WritesOneEntryWithItsLineCount()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        var sale = await SellTwoUnitsAsync(dbContext, stock);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditEntityType.Sale, entry.EntityType);
        Assert.Equal(sale.Id.ToString(), entry.EntityId);
        Assert.Equal($"{sale.SaleNumber} (2 lignes)", entry.EntityLabel);
        Assert.Equal(_accountId, entry.AccountId);
        Assert.Null(entry.DeletedContent);
    }

    [Fact]
    public async Task DeletingASale_KeepsWhatItContained()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var sale = await SellTwoUnitsAsync(dbContext, stock);
        var marker = await MarkAsync();

        await new SaleService(dbContext).DeleteAsync(sale.Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Deleted, entry.Action);
        Assert.Equal(AuditEntityType.Sale, entry.EntityType);

        using var content = JsonDocument.Parse(entry.DeletedContent!);
        var root = content.RootElement;
        Assert.Equal(sale.SaleNumber, root.GetProperty("saleNumber").GetString());
        Assert.Equal("Jean Dupont", root.GetProperty("customerName").GetString());
        Assert.True(root.GetProperty("paid").GetBoolean());
        Assert.Equal(30m, root.GetProperty("total").GetDecimal());
        var lines = root.GetProperty("lines").EnumerateArray().ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal(stock.Units[0].UnitNumber, lines[0].GetProperty("unitNumber").GetString());
        Assert.Equal("Jambon sec", lines[0].GetProperty("productName").GetString());
        Assert.Equal(20m, lines[0].GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task CorrectingThePaymentOfASale_WritesAnUpdate()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var sale = await SellTwoUnitsAsync(dbContext, stock);
        var marker = await MarkAsync();

        await new SaleService(dbContext).SetPaymentAsync(sale.Id, new SetSalePaymentRequest { Paid = false });

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Updated, entry.Action);
        Assert.Equal(sale.SaleNumber, entry.EntityLabel);
    }

    [Fact]
    public async Task CorrectingThenRemovingASaleLine_WritesOneEntryEach_WithoutUnitChanges()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var sale = await SellTwoUnitsAsync(dbContext, stock);
        var marker = await MarkAsync();
        var movements = new StockMovementService(dbContext);

        await movements.UpdateAsync(sale.Lines[0].Id, new UpdateStockMovementRequest { SoldWeight = 1.000m, Amount = 18m });
        await movements.DeleteAsync(sale.Lines[1].Id);

        var entries = await EntriesAfterAsync(marker);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal(AuditEntityType.StockMovement, e.EntityType));
        Assert.Equal(AuditAction.Updated, entries[0].Action);
        Assert.Equal($"{sale.SaleNumber} · {stock.Units[0].UnitNumber}", entries[0].EntityLabel);
        Assert.Equal(AuditAction.Deleted, entries[1].Action);
        using var content = JsonDocument.Parse(entries[1].DeletedContent!);
        Assert.Equal(10m, content.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal("sale", content.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task RecordingABatchThenWeighingItsUnits_WritesTwoEntries()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        var batch = await new ProductionBatchService(dbContext).CreateAsync(new CreateProductionBatchRequest
        {
            ProductId = stock.Product.Id,
            ProductionDate = ProductionDate,
            SalePrice = 21m,
        });
        var units = await new StockUnitService(dbContext).AddUnitsAsync(
            batch.Id, new AddStockUnitsRequest { Weights = [0.900m, 0.800m] });

        var entries = await EntriesAfterAsync(marker);
        Assert.Equal(2, entries.Count);
        Assert.Equal(AuditEntityType.ProductionBatch, entries[0].EntityType);
        Assert.Equal("Jambon sec — 13/09/2026", entries[0].EntityLabel);
        Assert.Equal(batch.Id.ToString(), entries[0].EntityId);
        Assert.Equal(AuditEntityType.StockUnit, entries[1].EntityType);
        Assert.Equal($"Jambon sec — 13/09/2026 (2 : {units[0].UnitNumber} à {units[1].UnitNumber})", entries[1].EntityLabel);
        Assert.Null(entries[1].EntityId);
    }

    [Fact]
    public async Task DeletingABatch_KeepsItsUnitsInOneEntry()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        await new ProductionBatchService(dbContext).DeleteAsync(stock.Batch.Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Deleted, entry.Action);
        Assert.Equal(AuditEntityType.ProductionBatch, entry.EntityType);
        using var content = JsonDocument.Parse(entry.DeletedContent!);
        Assert.Equal(20m, content.RootElement.GetProperty("salePrice").GetDecimal());
        Assert.Equal(3, content.RootElement.GetProperty("units").GetArrayLength());
    }

    [Fact]
    public async Task DeletingAUnit_KeepsItsWeight()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        await new StockUnitService(dbContext).DeleteAsync(stock.Units[2].Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditEntityType.StockUnit, entry.EntityType);
        Assert.Equal(stock.Units[2].UnitNumber, entry.EntityLabel);
        using var content = JsonDocument.Parse(entry.DeletedContent!);
        Assert.Equal(0.400m, content.RootElement.GetProperty("weight").GetDecimal());
        Assert.Equal("Jambon sec", content.RootElement.GetProperty("productName").GetString());
    }

    [Fact]
    public async Task APersonalOutcome_WritesOneEntryAndNoUnitChange()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        await new StockMovementService(dbContext).CreateAsync(
            stock.Units[2].Id, new CreateStockMovementRequest { Type = MovementType.Personal });

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditEntityType.StockMovement, entry.EntityType);
        Assert.Equal($"Perso · {stock.Units[2].UnitNumber}", entry.EntityLabel);
    }

    [Fact]
    public async Task WritingOffTheStockOfAProduct_WritesOneGroupedEntry()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        var marker = await MarkAsync();

        await new ProductService(dbContext).WriteOffStockAsync(
            stock.Product.Id, new WriteOffProductStockRequest { StockUnitIds = stock.Units.Select(u => u.Id).ToList() });

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditEntityType.StockMovement, entry.EntityType);
        Assert.Equal("Perte · Jambon sec (3 unités)", entry.EntityLabel);
        Assert.Null(entry.EntityId);
    }

    [Fact]
    public async Task ClosingAnOpenedUnit_IsRecorded()
    {
        await using var dbContext = CreateContext();
        var stock = await SeedStockAsync(dbContext);
        await new SaleService(dbContext).CreateAsync(new CreateSaleRequest
        {
            CustomerId = stock.Customer.Id,
            Lines = [new CreateSaleLineRequest { StockUnitId = stock.Units[0].Id, IsFullSale = false, SoldWeight = 0.200m, Amount = 4m }],
        });
        var marker = await MarkAsync();

        await new StockMovementService(dbContext).CloseAsync(stock.Units[0].Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Updated, entry.Action);
        Assert.Equal(AuditEntityType.StockUnit, entry.EntityType);
        Assert.Equal($"{stock.Units[0].UnitNumber} — clôturée", entry.EntityLabel);
    }

    [Fact]
    public async Task DeactivatingAProduct_SaysSoInTheLabel()
    {
        await using var dbContext = CreateContext();
        var products = new ProductService(dbContext);
        var product = await products.CreateAsync(new CreateProductRequest { Code = "TC", Name = "Terrine", SaleMode = SaleMode.ByPiece });
        var marker = await MarkAsync();

        await products.DeactivateAsync(product.Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Updated, entry.Action);
        Assert.Equal("Terrine (TC) — désactivé", entry.EntityLabel);
    }

    [Fact]
    public async Task DeletingACustomer_KeepsItsDetails()
    {
        await using var dbContext = CreateContext();
        var customers = new CustomerService(dbContext);
        var customer = await customers.CreateAsync(new CreateCustomerRequest { LastName = "Perrin", FirstName = "Marie", Phone = "0600000000" });
        var marker = await MarkAsync();

        await customers.DeleteAsync(customer.Id);

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Equal(AuditAction.Deleted, entry.Action);
        Assert.Equal("Marie Perrin", entry.EntityLabel);
        using var content = JsonDocument.Parse(entry.DeletedContent!);
        Assert.Equal("0600000000", content.RootElement.GetProperty("phone").GetString());
    }

    [Fact]
    public async Task AccountGestures_AreRecordedWithoutTechnicalNoise()
    {
        await using var dbContext = CreateContext();
        var accounts = new AccountService(dbContext, PostgresDatabaseFixture.CreateUserManager(dbContext));
        var marker = await MarkAsync();

        var created = await accounts.CreateAsync(new CreateAccountRequest
        {
            Email = "gerard@saloir.local",
            DisplayName = "Gérard",
            Password = UserPassword,
        });
        await accounts.UpdateAsync(created.Id, new UpdateAccountRequest
        {
            DisplayName = "Gérard",
            Role = AccountRole.Admin,
            NewPassword = AdminPassword,
        });
        await accounts.ResetPasswordAsync(created.Id, AdminPassword + "!");

        var entries = await EntriesAfterAsync(marker);
        Assert.All(entries, e => Assert.Equal(AuditEntityType.Account, e.EntityType));
        Assert.All(entries, e => Assert.Equal(_accountId, e.AccountId));
        Assert.Equal(
            [AuditAction.Created, AuditAction.PasswordChanged, AuditAction.Updated, AuditAction.PasswordChanged],
            entries.Select(e => e.Action));
        Assert.Equal("Gérard (gerard@saloir.local)", entries[0].EntityLabel);
        Assert.Equal("Gérard — rôle : Administrateur", entries[2].EntityLabel);
    }

    [Fact]
    public async Task WithoutCurrentAccount_TheEntryHasNoAuthor()
    {
        await using var dbContext = fixture.CreateDbContext();
        var marker = await MarkAsync();

        await new CustomerService(dbContext).CreateAsync(new CreateCustomerRequest { LastName = "Perrin" });

        var entry = Assert.Single(await EntriesAfterAsync(marker));
        Assert.Null(entry.AccountId);
    }

    [Fact]
    public async Task AFailedSave_LeavesNoEntry()
    {
        await using var dbContext = CreateContext();
        await new ProductService(dbContext).CreateAsync(new CreateProductRequest { Code = "TC", Name = "Terrine", SaleMode = SaleMode.ByPiece });
        var marker = await MarkAsync();

        await using var failing = CreateContext();
        failing.Products.Add(new Product { Code = "TC", Name = "Doublon", SaleMode = SaleMode.ByPiece });

        await Assert.ThrowsAsync<DbUpdateException>(() => failing.SaveChangesAsync());
        Assert.Empty(await EntriesAfterAsync(marker));
    }
}
