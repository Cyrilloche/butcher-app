using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

/// <summary>
/// Journal « qui a fait quoi », réservé à l'administrateur (FR-023). Lecture seule : aucune route ne
/// le modifie ni ne le supprime (FR-024).
/// </summary>
[ApiController]
[Route("api/audit-entries")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AuditEntriesController(IAuditEntryService auditEntryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditEntryPageDto>> Search([FromQuery] AuditEntryQuery query)
    {
        return Ok(await auditEntryService.SearchAsync(query));
    }
}
