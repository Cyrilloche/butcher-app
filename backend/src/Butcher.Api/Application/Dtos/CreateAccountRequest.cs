using System.ComponentModel.DataAnnotations;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

/// <summary>
/// Création d'un compte par l'administrateur (FR-003). Le mot de passe initial est choisi par
/// l'administrateur et transmis de vive voix : l'outil n'envoie aucun message.
/// </summary>
public class CreateAccountRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MaxLength(100)]
    public required string DisplayName { get; set; }

    public AccountRole Role { get; set; } = AccountRole.User;

    [Required]
    public required string Password { get; set; }
}
