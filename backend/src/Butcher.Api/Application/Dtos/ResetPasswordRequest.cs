using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

/// <summary>Réinitialisation du mot de passe d'un compte par l'administrateur (FR-003).</summary>
public class ResetPasswordRequest
{
    [Required]
    public required string NewPassword { get; set; }
}
