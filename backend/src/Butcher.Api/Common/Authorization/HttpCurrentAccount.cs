using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Lit le compte dans le jeton de la requête HTTP en cours.
/// </summary>
public sealed class HttpCurrentAccount(IHttpContextAccessor httpContextAccessor) : ICurrentAccount
{
    public Guid? AccountId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
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
}
