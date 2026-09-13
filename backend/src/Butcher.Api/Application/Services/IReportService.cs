using Butcher.Api.Application.Dtos.Reports;

namespace Butcher.Api.Application.Services;

/// <summary>Rapports de ventes, en lecture seule (FR-027 à FR-031). Périodes : jours de Paris, bornes incluses.</summary>
public interface IReportService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly from, DateOnly to);

    Task<List<CustomerSalesDto>> GetSalesByCustomerAsync(DateOnly from, DateOnly to);

    Task<List<ProductSalesDto>> GetSalesByProductAsync(DateOnly from, DateOnly to);

    Task<ReceivablesDto> GetReceivablesAsync();
}
