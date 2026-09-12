using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        return Ok(await productService.GetAllAsync(includeInactive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        return Ok(await productService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        var created = await productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request)
    {
        return Ok(await productService.UpdateAsync(id, request));
    }

    // Désactiver, réactiver et solder le stock engagent le catalogue entier : gestes réservés à
    // l'administrateur (FR-011). Les corrections de saisie, elles, restent ouvertes à tous.
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await productService.DeactivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/write-off")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<WriteOffProductStockResult>> WriteOffStock(
        int id, WriteOffProductStockRequest request)
    {
        return Ok(await productService.WriteOffStockAsync(id, request));
    }

    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Reactivate(int id)
    {
        await productService.ReactivateAsync(id);
        return NoContent();
    }
}
