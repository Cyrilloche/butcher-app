using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/units-of-measure")]
public class UnitsOfMeasureController(IUnitOfMeasureService unitOfMeasureService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UnitOfMeasureDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        return Ok(await unitOfMeasureService.GetAllAsync(includeInactive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnitOfMeasureDto>> GetById(int id)
    {
        return Ok(await unitOfMeasureService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<UnitOfMeasureDto>> Create(CreateUnitOfMeasureRequest request)
    {
        var created = await unitOfMeasureService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitOfMeasureDto>> Update(int id, UpdateUnitOfMeasureRequest request)
    {
        return Ok(await unitOfMeasureService.UpdateAsync(id, request));
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await unitOfMeasureService.DeactivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id)
    {
        await unitOfMeasureService.ReactivateAsync(id);
        return NoContent();
    }
}
