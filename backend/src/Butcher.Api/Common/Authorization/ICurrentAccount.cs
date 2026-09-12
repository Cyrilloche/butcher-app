namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Compte à l'origine de l'opération en cours (ADR-011).
/// </summary>
public interface ICurrentAccount
{
    /// <summary>
    /// Identifiant du compte authentifié, ou <c>null</c> hors requête authentifiée (commande hors
    /// ligne, migration, démarrage).
    /// </summary>
    Guid? AccountId { get; }
}
