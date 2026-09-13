using System.Text.Json;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

/// <summary>Entrée du journal, en lecture (specs/005-backoffice, contracts §6).</summary>
public class AuditEntryDto
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? AccountId { get; set; }

    /// <summary>Nom affiché de l'auteur ; <c>null</c> sans auteur.</summary>
    public string? AccountName { get; set; }

    public AuditAction Action { get; set; }

    public AuditEntityType? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? EntityLabel { get; set; }

    /// <summary>Contenu de l'objet supprimé ; seulement pour une suppression (FR-022).</summary>
    public JsonElement? DeletedContent { get; set; }
}

public class AuditEntryPageDto
{
    public List<AuditEntryDto> Items { get; set; } = [];

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

/// <summary>Filtres du journal. Les types et natures arrivent en <c>snake_case</c>, comme ils sont affichés par l'API.</summary>
public class AuditEntryQuery
{
    public Guid? AccountId { get; set; }

    public string? EntityType { get; set; }

    public string? Action { get; set; }

    /// <summary>Jour de début inclus, à Paris.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Jour de fin inclus, à Paris.</summary>
    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
