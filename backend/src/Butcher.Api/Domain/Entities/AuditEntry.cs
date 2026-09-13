using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Trace d'un geste : qui, quand, quoi (FR-021). Immuable : aucune route ne la modifie ni ne la
/// supprime (FR-024).
/// </summary>
/// <remarks>
/// Écrite par <c>AppDbContext.SaveChanges</c>, dans la transaction de l'opération qu'elle trace
/// (specs/005-backoffice, research R-09) ; seules les connexions sont écrites explicitement, par
/// <c>AuthService</c>.
/// </remarks>
public class AuditEntry
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Auteur ; <c>null</c> hors requête (commande hors ligne) ou sur une adresse inconnue.</summary>
    public Guid? AccountId { get; set; }

    public AppUser? Account { get; set; }

    public AuditAction Action { get; set; }

    /// <summary><c>null</c> pour une connexion refusée sur une adresse qui ne correspond à aucun compte.</summary>
    public AuditEntityType? EntityType { get; set; }

    /// <summary>Identifiant de l'objet, entier ou uuid en texte ; <c>null</c> pour un geste groupé.</summary>
    public string? EntityId { get; set; }

    /// <summary>Libellé lisible au moment du geste, en français.</summary>
    public string? EntityLabel { get; set; }

    /// <summary>Contenu JSON de l'objet supprimé, suffisant pour le ressaisir ; seulement pour une suppression (FR-022).</summary>
    public string? DeletedContent { get; set; }
}
