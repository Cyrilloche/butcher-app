using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(ISaleService saleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SaleDto>>> GetAll(
        [FromQuery] int? customerId = null,
        [FromQuery] bool? paid = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        return Ok(await saleService.GetAllAsync(customerId, paid, from, to));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleDto>> GetById(int id)
    {
        return Ok(await saleService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(CreateSaleRequest request)
    {
        var created = await saleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SaleDto>> Update(int id, UpdateSaleRequest request)
    {
        return Ok(await saleService.UpdateAsync(id, request));
    }

    [HttpPost("{id:int}/payment")]
    public async Task<ActionResult<SaleDto>> SetPayment(int id, SetSalePaymentRequest request)
    {
        return Ok(await saleService.SetPaymentAsync(id, request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await saleService.DeleteAsync(id);
        return NoContent();
    }
}
