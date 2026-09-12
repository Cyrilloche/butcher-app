using Butcher.Api.Common.Exceptions;

namespace Butcher.Api.Common.Authorization;

public static class CurrentAccountExtensions
{
    /// <summary>
    /// Identifiant du compte de la requête. Sur une route protégée il est toujours présent ; son absence
    /// signale un jeton sans identifiant exploitable.
    /// </summary>
    public static Guid RequireAccountId(this ICurrentAccount currentAccount) =>
        currentAccount.AccountId ?? throw new UnauthorizedException("Session invalide.");
}
