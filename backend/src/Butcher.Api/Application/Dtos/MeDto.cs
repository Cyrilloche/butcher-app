using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

/// <summary>
/// Compte connecté, relu en base (ADR-011). L'interface s'en sert pour adapter ce qu'elle propose ;
/// le serveur, lui, vérifie les droits à chaque requête.
/// </summary>
public class MeDto
{
    public required Guid Id { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required AccountRole Role { get; set; }

    /// <summary>L'interface ne propose « Dicter » que si l'assistant est activé (FR-001) ; le serveur refuse sinon.</summary>
    public required bool AssistantEnabled { get; set; }
}
