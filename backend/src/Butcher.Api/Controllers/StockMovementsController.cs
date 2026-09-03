using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
public class StockMovementsController(IStockMovementService stockMovementService) : ControllerBase
{
    [HttpGet("/api/stock-movements")]
    public async Task<ActionResult<List<StockMovementDto>>> GetAll(
        [FromQuery] int? stockUnitId = null, [FromQuery] int? customerId = null)
    {
        return Ok(await stockMovementService.GetAllAsync(stockUnitId, customerId));
    }

    [HttpGet("/api/stock-movements/{id:int}")]
    public async Task<ActionResult<StockMovementDto>> GetById(int id)
    {
        return Ok(await stockMovementService.GetByIdAsync(id));
    }

    [HttpPost("/api/stock-units/{stockUnitId:int}/movements")]
    public async Task<ActionResult<StockMovementDto>> Create(int stockUnitId, CreateStockMovementRequest request)
    {
        var created = await stockMovementService.CreateAsync(stockUnitId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("/api/stock-movements/{id:int}")]
    public async Task<ActionResult<StockMovementDto>> Update(int id, UpdateStockMovementRequest request)
    {
        return Ok(await stockMovementService.UpdateAsync(id, request));
    }

    [HttpDelete("/api/stock-movements/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await stockMovementService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("/api/stock-units/{stockUnitId:int}/close")]
    public async Task<IActionResult> Close(int stockUnitId)
    {
        await stockMovementService.CloseAsync(stockUnitId);
        return NoContent();
    }
}
