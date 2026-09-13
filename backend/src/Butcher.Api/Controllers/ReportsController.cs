using Butcher.Api.Application.Dtos.Reports;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

/// <summary>Rapports de ventes, réservés à l'administrateur (FR-027 à FR-029, contracts §7).</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("sales-summary")]
    public async Task<ActionResult<SalesSummaryDto>> SalesSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = RequirePeriod(from, to);
        return Ok(await reportService.GetSalesSummaryAsync(start, end));
    }

    [HttpGet("sales-by-customer")]
    public async Task<ActionResult<List<CustomerSalesDto>>> SalesByCustomer([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = RequirePeriod(from, to);
        return Ok(await reportService.GetSalesByCustomerAsync(start, end));
    }

    [HttpGet("sales-by-product")]
    public async Task<ActionResult<List<ProductSalesDto>>> SalesByProduct([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = RequirePeriod(from, to);
        return Ok(await reportService.GetSalesByProductAsync(start, end));
    }

    [HttpGet("receivables")]
    public async Task<ActionResult<ReceivablesDto>> Receivables()
    {
        return Ok(await reportService.GetReceivablesAsync());
    }

    private static (DateOnly From, DateOnly To) RequirePeriod(DateOnly? from, DateOnly? to) =>
        from is { } start && to is { } end
            ? (start, end)
            : throw new BadRequestException("Choisissez une période : une date de début et une date de fin.");
}
