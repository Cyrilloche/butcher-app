using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

/// <summary>Gestion des comptes, réservée à l'administrateur (ADR-011, FR-003).</summary>
[ApiController]
[Route("api/accounts")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AccountsController(IAccountService accountService, ICurrentAccount currentAccount) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAll()
    {
        return Ok(await accountService.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountRequest request)
    {
        var created = await accountService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(Guid id, UpdateAccountRequest request)
    {
        return Ok(await accountService.UpdateAsync(id, request));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await accountService.DeactivateAsync(id, actingAccountId: currentAccount.RequireAccountId());
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await accountService.ReactivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        await accountService.ResetPasswordAsync(id, request.NewPassword);
        return NoContent();
    }
}
