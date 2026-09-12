using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

/// <summary>Un compte, vu par l'administrateur (ADR-011).</summary>
public class AccountDto
{
    public required Guid Id { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required AccountRole Role { get; set; }

    public required bool IsActive { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
