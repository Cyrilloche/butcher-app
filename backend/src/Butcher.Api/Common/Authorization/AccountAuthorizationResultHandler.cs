using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Traduit les refus de <see cref="AccountAuthorizationHandler"/> en réponses explicites, avec un
/// message en français, au lieu d'un <c>403</c> vide.
/// </summary>
public sealed class AccountAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var reasons = authorizeResult.AuthorizationFailure?.FailureReasons.Select(r => r.Message).ToHashSet()
            ?? [];

        if (reasons.Contains(AccountAuthorizationHandler.InactiveAccountReason))
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Non autorisé", "Ce compte est désactivé.");
            return;
        }

        if (reasons.Contains(AccountAuthorizationHandler.AdminRequiredReason))
        {
            await WriteProblemAsync(
                context, StatusCodes.Status403Forbidden, "Accès réservé", "Action réservée à l'administrateur.");
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = detail });
    }
}
