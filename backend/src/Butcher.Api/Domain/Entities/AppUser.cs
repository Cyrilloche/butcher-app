namespace Butcher.Api.Domain.Entities;

// Placeholder simple : sera reconcilié avec ASP.NET Core Identity lors du spike auth (ADR-009).
public class AppUser
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
