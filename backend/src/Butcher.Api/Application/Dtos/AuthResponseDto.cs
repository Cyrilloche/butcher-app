namespace Butcher.Api.Application.Dtos;

// Ne contient JAMAIS le refresh token (vit uniquement dans un cookie httpOnly, jamais dans un corps JSON).
public class AuthResponseDto
{
    public required string AccessToken { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
