using System.Globalization;
using Butcher.Api.Application.Dtos.Reports;
using Butcher.Api.Common;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

/// <summary>
/// Rapports de ventes calculés à la demande, sans donnée stockée (specs/005-backoffice, data-model §4).
/// </summary>
/// <remarks>
/// Le montant d'une vente est la somme des montants saisis sur ses lignes (FR-030) : le prix encaissé
/// en espèces peut différer du poids multiplié par le prix, et c'est lui qui compte. Aucune notion de
/// coût ni de marge (FR-031). Les sommes sont faites en <see cref="decimal"/>, exactes au centime.
/// Les ventes sont regroupées en mémoire : le volume d'une activité artisanale le permet, et les jours
/// et mois de Paris ne se calculent pas simplement en SQL.
/// </remarks>
public class ReportService(AppDbContext dbContext) : IReportService
{
    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly from, DateOnly to)
    {
        var sales = await SalesInPeriodAsync(from, to);

        return new SalesSummaryDto
        {
            SaleCount = sales.Count,
            Total = sales.Sum(s => s.Total),
            PaidTotal = sales.Where(s => s.Paid).Sum(s => s.Total),
            PendingTotal = sales.Where(s => !s.Paid).Sum(s => s.Total),
            Months = sales
                .GroupBy(s => BusinessTime.DayOf(s.Date).ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new MonthlySalesDto
                {
                    Month = g.Key,
                    SaleCount = g.Count(),
                    Total = g.Sum(s => s.Total),
                    PaidTotal = g.Where(s => s.Paid).Sum(s => s.Total),
                    PendingTotal = g.Where(s => !s.Paid).Sum(s => s.Total),
                })
                .ToList(),
        };
    }

    public async Task<List<CustomerSalesDto>> GetSalesByCustomerAsync(DateOnly from, DateOnly to)
    {
        var sales = await SalesInPeriodAsync(from, to);

        return sales
            .GroupBy(s => s.CustomerId)
            .Select(g => new CustomerSalesDto
            {
                CustomerId = g.Key,
                CustomerName = g.First().CustomerName,
                SaleCount = g.Count(),
                Total = g.Sum(s => s.Total),
                PendingTotal = g.Where(s => !s.Paid).Sum(s => s.Total),
            })
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.CustomerName, StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true))
            .ToList();
    }

    public async Task<List<ProductSalesDto>> GetSalesByProductAsync(DateOnly from, DateOnly to)
    {
        var (start, end) = Period(from, to);

        var lines = await dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.Type == MovementType.Sale && m.Sale != null && m.Sale.Date >= start && m.Sale.Date < end)
            .Select(m => new
            {
                m.StockUnit!.Batch!.ProductId,
                m.StockUnit.Batch.Product!.Code,
                m.StockUnit.Batch.Product.Name,
                m.StockUnit.Batch.Product.SaleMode,
                m.StockUnitId,
                m.SoldWeight,
                m.Amount,
            })
            .ToListAsync();

        return lines
            .GroupBy(l => l.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new ProductSalesDto
                {
                    ProductId = g.Key,
                    ProductCode = first.Code,
                    ProductName = first.Name,
                    SaleMode = first.SaleMode,
                    UnitCount = g.Select(l => l.StockUnitId).Distinct().Count(),
                    LineCount = g.Count(),
                    SoldWeight = first.SaleMode == SaleMode.ByWeight ? g.Sum(l => l.SoldWeight ?? 0m) : null,
                    Total = g.Sum(l => l.Amount ?? 0m),
                };
            })
            .OrderByDescending(p => p.Total)
            .ThenBy(p => p.ProductName, StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true))
            .ToList();
    }

    public async Task<ReceivablesDto> GetReceivablesAsync()
    {
        var unpaid = await ProjectSales(dbContext.Sales.AsNoTracking().Where(s => !s.Paid)).ToListAsync();

        var customers = unpaid
            .GroupBy(s => s.CustomerId)
            .Select(g => new CustomerReceivableDto
            {
                CustomerId = g.Key,
                CustomerName = g.First().CustomerName,
                PendingTotal = g.Sum(s => s.Total),
                OldestUnpaidDate = g.Min(s => s.Date),
                Sales = g
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.Id)
                    .Select(s => new UnpaidSaleDto { Id = s.Id, SaleNumber = s.SaleNumber, Date = s.Date, Total = s.Total })
                    .ToList(),
            })
            .OrderByDescending(c => c.PendingTotal)
            .ThenBy(c => c.OldestUnpaidDate)
            .ToList();

        return new ReceivablesDto { Total = customers.Sum(c => c.PendingTotal), Customers = customers };
    }

    private sealed record SaleRow(int Id, string SaleNumber, DateTimeOffset Date, bool Paid, int CustomerId, string CustomerName, decimal Total);

    private async Task<List<SaleRow>> SalesInPeriodAsync(DateOnly from, DateOnly to)
    {
        var (start, end) = Period(from, to);
        return await ProjectSales(dbContext.Sales.AsNoTracking().Where(s => s.Date >= start && s.Date < end)).ToListAsync();
    }

    private static IQueryable<SaleRow> ProjectSales(IQueryable<Domain.Entities.Sale> sales) =>
        sales.Select(s => new SaleRow(
            s.Id,
            s.SaleNumber,
            s.Date,
            s.Paid,
            s.CustomerId,
            (s.Customer!.FirstName == null ? "" : s.Customer.FirstName + " ") + s.Customer.LastName,
            s.StockMovements.Sum(m => m.Amount ?? 0m)));

    /// <summary>Instants de début (inclus) et de fin (exclu) de la période, jours de Paris.</summary>
    private static (DateTimeOffset Start, DateTimeOffset End) Period(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new BadRequestException("La date de début doit précéder la date de fin.");
        }

        return (BusinessTime.StartOfDay(from), BusinessTime.StartOfDay(to.AddDays(1)));
    }
}
