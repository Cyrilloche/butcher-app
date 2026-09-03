using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
