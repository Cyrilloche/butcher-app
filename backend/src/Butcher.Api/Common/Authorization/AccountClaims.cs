using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Butcher.Api.Common.Authorization;

/// <summary>Lecture du compte dans les claims d'un jeton d'accès.</summary>
public static class AccountClaims
{
    /// <summary>
    /// Identifiant du compte, ou <c>null</c> si le principal n'est pas authentifié ou ne porte pas
    /// d'identifiant valide.
    /// </summary>
    public static Guid? GetAccountId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Le gestionnaire JWT peut remapper « sub » vers NameIdentifier : on accepte les deux.
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var accountId) ? accountId : null;
    }
}
