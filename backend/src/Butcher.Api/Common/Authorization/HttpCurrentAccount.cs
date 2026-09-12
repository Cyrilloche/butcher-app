namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Lit le compte dans le jeton de la requête HTTP en cours.
/// </summary>
public sealed class HttpCurrentAccount(IHttpContextAccessor httpContextAccessor) : ICurrentAccount
{
    public Guid? AccountId => AccountClaims.GetAccountId(httpContextAccessor.HttpContext?.User);
}
