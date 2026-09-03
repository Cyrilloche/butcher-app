using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAuthService authService, IConfiguration configuration) : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(new AuthResponseDto { AccessToken = result.AccessToken, ExpiresAtUtc = result.AccessTokenExpiresAtUtc });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshCookieName]
            ?? throw new UnauthorizedException("Aucun jeton de rafraîchissement fourni.");

        var result = await authService.RefreshAsync(refreshToken);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(new AuthResponseDto { AccessToken = result.AccessToken, ExpiresAtUtc = result.AccessTokenExpiresAtUtc });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            await authService.LogoutAsync(refreshToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

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
