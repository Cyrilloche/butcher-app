using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Une demande faite à l'assistant vocal (RF-36, FR-024) : qui, quand, ce qui a été entendu, l'issue et
/// la durée de traitement. Jamais l'audio. Immuable : aucune route ne la modifie ni ne la supprime.
/// </summary>
/// <remarks>
/// Distincte d'<see cref="AuditEntry"/> : une demande ne change aucune donnée, et ses champs servent à
/// mesurer l'usage, le délai et les ratés (specs/006-assistant-vocal, research R-02). Écrite par
/// <c>AssistantService</c> seul.
/// </remarks>
public class VoiceRequest
{
    /// <summary>Identifiant ; sert aussi à demander la voix de la réponse (research R-05).</summary>
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public AppUser? Account { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public VoiceInputMode InputMode { get; set; }

    /// <summary>
    /// Phrase entendue ou écrite ; <c>null</c> si la transcription a échoué ou si la limite était atteinte.
    /// Peut contenir un nom de client : elle reste dans la base de l'application (clarification Q1).
    /// </summary>
    public string? HeardText { get; set; }

    public VoiceRequestOutcome Outcome { get; set; }

    /// <summary>Phrase de réponse, telle que dite ; <c>null</c> en erreur ou limite atteinte.</summary>
    public string? ReplySpeech { get; set; }

    /// <summary>Durée de traitement par le serveur, de la réception de la demande à la réponse.</summary>
    public int DurationMs { get; set; }
}
