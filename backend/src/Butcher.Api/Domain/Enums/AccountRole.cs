namespace Butcher.Api.Domain.Enums;

/// <summary>
/// Rôle d'un compte (ADR-011). Deux rôles fixes et exclusifs, sans droits à la carte.
/// </summary>
public enum AccountRole
{
    /// <summary>Usage courant : production, stock, ventes, clients, corrections de saisie.</summary>
    User,

    /// <summary>Tout ce que fait un utilisateur, plus les comptes, les gestes qui engagent le
    /// catalogue, le journal et les rapports.</summary>
    Admin,
}
