using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

/// <summary>
/// Rapports exacts au centime, calculés sur les montants enregistrés, périodes en jours de Paris
/// (FR-027 à FR-030, SC-006).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ReportServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private int _saleNumber;
    private Product _ham = null!;
    private Product _terrine = null!;
    private ProductionBatch _hamBatch = null!;
    private ProductionBatch _terrineBatch = null!;
    private Customer _jean = null!;
    private Customer _marie = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();

        await using var dbContext = fixture.CreateDbContext();
        _ham = new Product { Code = "JB", Name = "Jambon sec", SaleMode = SaleMode.ByWeight, AllowPartialSale = true };
        _terrine = new Product { Code = "TC", Name = "Terrine", SaleMode = SaleMode.ByPiece };
        _jean = new Customer { LastName = "Dupont", FirstName = "Jean" };
        _marie = new Customer { LastName = "Perrin", FirstName = "Marie" };
        dbContext.AddRange(_ham, _terrine, _jean, _marie);
        await dbContext.SaveChangesAsync();

        _hamBatch = new ProductionBatch { ProductId = _ham.Id, ProductionDate = new DateOnly(2026, 8, 1), SalePrice = 20m };
        _terrineBatch = new ProductionBatch { ProductId = _terrine.Id, ProductionDate = new DateOnly(2026, 8, 1), SalePrice = 8m };
        dbContext.AddRange(_hamBatch, _terrineBatch);
        await dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<StockUnit> SeedUnitAsync(ProductionBatch batch, decimal? weight)
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = new StockUnit { BatchId = batch.Id, UnitNumber = TestUnitNumber.Next(), Weight = weight };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit;
    }

    /// <summary>Vente enregistrée telle quelle : les montants sont ceux qui ont été saisis.</summary>
    private async Task<Sale> SeedSaleAsync(
        Customer customer, DateTimeOffset date, bool paid, params (StockUnit Unit, decimal? Weight, decimal Amount)[] lines)
    {
        await using var dbContext = fixture.CreateDbContext();
        var sale = new Sale { SaleNumber = $"V-TEST-{++_saleNumber}", CustomerId = customer.Id, Date = date, Paid = paid };
        foreach (var (unit, weight, amount) in lines)
        {
            sale.StockMovements.Add(new StockMovement
            {
                StockUnitId = unit.Id,
                Type = MovementType.Sale,
                Date = date,
                SoldWeight = weight,
                Amount = amount,
            });
        }

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();
        return sale;
    }

    private ReportService CreateSut() => new(fixture.CreateDbContext());

    private static DateTimeOffset Utc(int month, int day, int hour = 10) => new(2026, month, day, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SalesSummary_SumsRecordedAmountsToTheCent_ByMonth()
    {
        var unit = await SeedUnitAsync(_terrineBatch, null);
        await SeedSaleAsync(_jean, Utc(9, 1), paid: true, (unit, null, 10.10m), (unit, null, 0.20m));
        await SeedSaleAsync(_marie, Utc(9, 2), paid: false, (unit, null, 5.05m));
        await SeedSaleAsync(_jean, Utc(8, 15), paid: true, (unit, null, 12.34m));
        await SeedSaleAsync(_jean, new DateTimeOffset(2025, 12, 31, 10, 0, 0, TimeSpan.Zero), paid: true, (unit, null, 99m));

        var summary = await CreateSut().GetSalesSummaryAsync(Jan1, Dec31);

        Assert.Equal(3, summary.SaleCount);
        Assert.Equal(27.69m, summary.Total);
        Assert.Equal(22.64m, summary.PaidTotal);
        Assert.Equal(5.05m, summary.PendingTotal);
        Assert.Equal(["2026-08", "2026-09"], summary.Months.Select(m => m.Month));
        var september = summary.Months[1];
        Assert.Equal(2, september.SaleCount);
        Assert.Equal(15.35m, september.Total);
        Assert.Equal(10.30m, september.PaidTotal);
        Assert.Equal(5.05m, september.PendingTotal);
    }

    [Fact]
    public async Task SalesSummary_OnAnEmptyPeriod_IsZero()
    {
        var summary = await CreateSut().GetSalesSummaryAsync(Jan1, Dec31);

        Assert.Equal(0, summary.SaleCount);
        Assert.Equal(0m, summary.Total);
        Assert.Empty(summary.Months);
    }

    [Fact]
    public async Task SalesSummary_PeriodAndMonthsFollowParisDays()
    {
        var unit = await SeedUnitAsync(_terrineBatch, null);
        // 22 h 30 UTC le 30 septembre = 0 h 30 le 1er octobre à Paris.
        await SeedSaleAsync(_jean, Utc(9, 30, hour: 22).AddMinutes(30), paid: true, (unit, null, 8m));

        var service = CreateSut();
        var september = await service.GetSalesSummaryAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));
        var october = await service.GetSalesSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(0, september.SaleCount);
        Assert.Equal(1, october.SaleCount);
        Assert.Equal("2026-10", Assert.Single(october.Months).Month);
    }

    [Fact]
    public async Task SalesByCustomer_OrdersFromTheLargestTotal_WithWhatRemainsToPay()
    {
        var unit = await SeedUnitAsync(_terrineBatch, null);
        await SeedSaleAsync(_jean, Utc(9, 1), paid: true, (unit, null, 8m));
        await SeedSaleAsync(_marie, Utc(9, 2), paid: false, (unit, null, 12.50m));
        await SeedSaleAsync(_marie, Utc(9, 3), paid: true, (unit, null, 4m));

        var customers = await CreateSut().GetSalesByCustomerAsync(Jan1, Dec31);

        Assert.Equal(["Marie Perrin", "Jean Dupont"], customers.Select(c => c.CustomerName));
        Assert.Equal(2, customers[0].SaleCount);
        Assert.Equal(16.50m, customers[0].Total);
        Assert.Equal(12.50m, customers[0].PendingTotal);
        Assert.Equal(0m, customers[1].PendingTotal);
    }

    [Fact]
    public async Task SalesByProduct_AHamSoldInFiveSlices_CountsOneUnitAndFiveLines()
    {
        var ham = await SeedUnitAsync(_hamBatch, 5.000m);
        var lostHam = await SeedUnitAsync(_hamBatch, 4.000m);
        var terrine = await SeedUnitAsync(_terrineBatch, null);
        for (var slice = 1; slice <= 5; slice++)
        {
            await SeedSaleAsync(slice % 2 == 0 ? _jean : _marie, Utc(9, slice), paid: true, (ham, 0.300m, 6.10m));
        }

        await SeedSaleAsync(_jean, Utc(9, 10), paid: false, (terrine, null, 8m));

        // Une perte n'est pas une vente : elle n'apparaît dans aucun rapport.
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.StockMovements.Add(new StockMovement { StockUnitId = lostHam.Id, Type = MovementType.Loss, Date = Utc(9, 11), SoldWeight = 4m });
            await dbContext.SaveChangesAsync();
        }

        var products = await CreateSut().GetSalesByProductAsync(Jan1, Dec31);

        Assert.Equal(2, products.Count);
        var hamRow = products[0];
        Assert.Equal("JB", hamRow.ProductCode);
        Assert.Equal(1, hamRow.UnitCount);
        Assert.Equal(5, hamRow.LineCount);
        Assert.Equal(1.500m, hamRow.SoldWeight);
        Assert.Equal(30.50m, hamRow.Total);
        var terrineRow = products[1];
        Assert.Equal(SaleMode.ByPiece, terrineRow.SaleMode);
        Assert.Null(terrineRow.SoldWeight);
        Assert.Equal(8m, terrineRow.Total);
    }

    [Fact]
    public async Task Receivables_GroupUnpaidSalesByCustomer_WithTheOldestFirst()
    {
        var unit = await SeedUnitAsync(_terrineBatch, null);
        var oldest = await SeedSaleAsync(_marie, new DateTimeOffset(2025, 11, 2, 10, 0, 0, TimeSpan.Zero), paid: false, (unit, null, 3.33m));
        await SeedSaleAsync(_marie, Utc(9, 2), paid: false, (unit, null, 9.17m));
        await SeedSaleAsync(_jean, Utc(9, 3), paid: false, (unit, null, 4m));
        await SeedSaleAsync(_jean, Utc(9, 4), paid: true, (unit, null, 50m));

        var receivables = await CreateSut().GetReceivablesAsync();

        Assert.Equal(16.50m, receivables.Total);
        Assert.Equal(["Marie Perrin", "Jean Dupont"], receivables.Customers.Select(c => c.CustomerName));
        var marie = receivables.Customers[0];
        Assert.Equal(12.50m, marie.PendingTotal);
        Assert.Equal(oldest.Date, marie.OldestUnpaidDate);
        Assert.Equal([oldest.Id], marie.Sales.Take(1).Select(s => s.Id));
        Assert.Single(receivables.Customers[1].Sales);
    }

    [Fact]
    public async Task Reports_RefuseAReversedPeriod()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => CreateSut().GetSalesSummaryAsync(Dec31, Jan1));
    }
}
