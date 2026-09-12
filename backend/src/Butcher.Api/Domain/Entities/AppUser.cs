using Butcher.Api.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Compte nominatif (ADR-011). Jamais supprimé, seulement désactivé : il reste l'auteur des
/// enregistrements qu'il a créés.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>Nom montré à l'écran (« Mireille »). L'email, lui, ne sert qu'à se connecter.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public AccountRole Role { get; set; } = AccountRole.User;

    /// <summary>Faux : connexion, rafraîchissement et requêtes refusés (FR-007).</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
