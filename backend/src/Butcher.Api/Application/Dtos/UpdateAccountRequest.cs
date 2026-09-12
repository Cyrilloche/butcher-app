using System.ComponentModel.DataAnnotations;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

/// <summary>
/// Modification du nom et du rôle d'un compte. L'email, identifiant de connexion, ne change pas.
/// </summary>
public class UpdateAccountRequest
{
    [Required, MaxLength(100)]
    public required string DisplayName { get; set; }

    public AccountRole Role { get; set; }

    /// <summary>
    /// Obligatoire pour promouvoir un utilisateur administrateur, et conforme à la règle de ce rôle
    /// (FR-035). Ignoré sinon : le mot de passe se réinitialise par sa propre route.
    /// </summary>
    public string? NewPassword { get; set; }
}
