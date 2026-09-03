using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/production-batches")]
public class ProductionBatchesController(IProductionBatchService productionBatchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductionBatchDto>>> GetAll([FromQuery] int? productId = null)
    {
        return Ok(await productionBatchService.GetAllAsync(productId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductionBatchDto>> GetById(int id)
    {
        return Ok(await productionBatchService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ProductionBatchDto>> Create(CreateProductionBatchRequest request)
    {
        var created = await productionBatchService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductionBatchDto>> Update(int id, UpdateProductionBatchRequest request)
    {
        return Ok(await productionBatchService.UpdateAsync(id, request));
    }
}
