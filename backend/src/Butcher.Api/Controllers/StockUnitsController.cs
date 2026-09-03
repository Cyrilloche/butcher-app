using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/stock-units")]
public class StockUnitsController(IStockUnitService stockUnitService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StockUnitDto>>> GetAll(
        [FromQuery] int? batchId = null, [FromQuery] StockUnitStatus? status = null)
    {
        return Ok(await stockUnitService.GetAllAsync(batchId, status));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockUnitDto>> GetById(int id)
    {
        return Ok(await stockUnitService.GetByIdAsync(id));
    }

    [HttpPost("/api/production-batches/{batchId:int}/stock-units")]
    public async Task<ActionResult<List<StockUnitDto>>> AddUnits(int batchId, AddStockUnitsRequest request)
    {
        var created = await stockUnitService.AddUnitsAsync(batchId, request);
        return CreatedAtAction(nameof(GetAll), new { batchId }, created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await stockUnitService.DeleteAsync(id);
        return NoContent();
    }
}
