using Butcher.Api.Application.Dtos.Reports;

namespace Butcher.Api.Application.Services;

/// <summary>Rapports de ventes, en lecture seule (FR-027 à FR-031). Périodes : jours de Paris, bornes incluses.</summary>
public interface IReportService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly from, DateOnly to);

    Task<List<CustomerSalesDto>> GetSalesByCustomerAsync(DateOnly from, DateOnly to);

    Task<List<ProductSalesDto>> GetSalesByProductAsync(DateOnly from, DateOnly to);

    Task<ReceivablesDto> GetReceivablesAsync();

    /// <summary>Usage de l'assistant vocal par compte et par semaine de Paris (RF-36, FR-025).</summary>
    Task<List<AssistantUsageDto>> GetAssistantUsageAsync(DateOnly from, DateOnly to);

    /// <summary>Demandes à l'assistant, les plus récentes d'abord, avec la phrase entendue (FR-025).</summary>
    Task<List<AssistantRequestDto>> GetAssistantRequestsAsync(DateOnly from, DateOnly to, Guid? accountId, int limit);
}
