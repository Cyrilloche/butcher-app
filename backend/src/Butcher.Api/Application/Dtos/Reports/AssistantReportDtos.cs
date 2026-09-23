using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos.Reports;

/// <summary>
/// Usage de l'assistant vocal par compte et par semaine (RF-36, FR-025) : sert à suivre l'adoption et le
/// délai. Les issues sont des champs nommés, pas un dictionnaire : un enum en clé de dictionnaire ne suit
/// pas la sérialisation <c>snake_case</c> du reste de l'API.
/// </summary>
public class AssistantUsageDto
{
    public required Guid AccountId { get; set; }

    public required string AccountName { get; set; }

    /// <summary>Lundi de la semaine, jour de Paris.</summary>
    public required DateOnly WeekStart { get; set; }

    public required int Requests { get; set; }

    public required int StockAnswers { get; set; }

    public required int SaleDrafts { get; set; }

    public required int NotUnderstood { get; set; }

    public required int Errors { get; set; }

    public required int RateLimited { get; set; }

    /// <summary>Durée médiane de traitement par le serveur ; <c>null</c> si aucune demande n'a été traitée.</summary>
    public int? MedianDurationMs { get; set; }
}

/// <summary>Une demande à l'assistant, pour comprendre un raté (FR-025).</summary>
public class AssistantRequestDto
{
    public required long Id { get; set; }

    public required DateTimeOffset OccurredAt { get; set; }

    public required string AccountName { get; set; }

    public required VoiceInputMode InputMode { get; set; }

    public string? HeardText { get; set; }

    public required VoiceRequestOutcome Outcome { get; set; }

    public string? ReplySpeech { get; set; }

    public required int DurationMs { get; set; }
}
