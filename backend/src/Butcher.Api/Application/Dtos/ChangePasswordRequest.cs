using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

/// <summary>Changement de son propre mot de passe (FR-004) : l'actuel est exigé.</summary>
public class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    public required string NewPassword { get; set; }
}
