using Microsoft.AspNetCore.Authorization;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Politiques d'autorisation de l'API (ADR-011). Le jeton porte l'identité ; les droits sont relus en
/// base à chaque requête par <see cref="AccountAuthorizationHandler"/>.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Réservé à un compte administrateur actif.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Tout compte authentifié et actif : politique par défaut et de repli.</summary>
    public static AuthorizationPolicy ActiveAccount { get; } = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(AccountRequirement.Active)
        .Build();

    public static AuthorizationPolicy Admin { get; } = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(AccountRequirement.Administrator)
        .Build();
}
