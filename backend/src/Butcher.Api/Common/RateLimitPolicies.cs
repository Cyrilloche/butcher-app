using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Butcher.Api.Common;

public static class RateLimitPolicies
{
    public const string Login = "login";

    /// <summary>Essais de connexion acceptés par adresse IP sur une fenêtre d'une minute.</summary>
    public const int LoginPermitPerMinute = 10;

    /// <summary>
    /// Freine l'essai en rafale sur <c>/api/auth/login</c>, par adresse IP. Complète le verrouillage
    /// du compte (<c>IdentityPolicy</c>) : lui protège le compte, ceci protège le serveur et
    /// ralentit qui essaierait plusieurs comptes.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Trop de tentatives",
                    Detail = "Trop de tentatives de connexion. Réessayez dans une minute.",
                }, cancellationToken);
            };

            options.AddPolicy(Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientAddress(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = LoginPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

    /// <summary>
    /// En prod, la requête traverse le tunnel Cloudflare puis Caddy : l'adresse vue par l'API est
    /// celle d'un conteneur, identique pour tous. L'IP réelle est dans <c>CF-Connecting-IP</c>, que
    /// Cloudflare écrase à chaque requête. Le client ne peut pas la falsifier : Caddy n'expose aucun
    /// port, seul le tunnel l'atteint.
    /// </summary>
    private static string ClientAddress(HttpContext httpContext) =>
        httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
}
