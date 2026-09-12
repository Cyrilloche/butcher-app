using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentAccount currentAccount, IConfiguration configuration)
    : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    // Seules login, refresh et logout sont ouvertes : [AllowAnonymous] est posé action par action, pour
    // que les routes du compte connecté restent soumises à la politique par défaut (compte actif).
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(new AuthResponseDto { AccessToken = result.AccessToken, ExpiresAtUtc = result.AccessTokenExpiresAtUtc });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshCookieName]
            ?? throw new UnauthorizedException("Aucun jeton de rafraîchissement fourni.");

        var result = await authService.RefreshAsync(refreshToken);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(new AuthResponseDto { AccessToken = result.AccessToken, ExpiresAtUtc = result.AccessTokenExpiresAtUtc });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            await authService.LogoutAsync(refreshToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    /// <summary>Compte connecté, relu en base (ADR-011).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        return Ok(await authService.GetAccountAsync(RequireAccountId()));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        await authService.ChangePasswordAsync(
            RequireAccountId(), request.CurrentPassword, request.NewPassword, refreshToken);
        return NoContent();
    }

    private Guid RequireAccountId() =>
        currentAccount.AccountId ?? throw new UnauthorizedException("Session invalide.");

    private void SetRefreshCookie(string value, DateTimeOffset expiresAt)
    {
        var sameSite = Enum.Parse<SameSiteMode>(configuration.GetValue("Auth:RefreshCookieSameSite", "Lax")!, ignoreCase: true);

        Response.Cookies.Append(RefreshCookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Expires = expiresAt,
            Path = "/api/auth",
        });
    }
}
