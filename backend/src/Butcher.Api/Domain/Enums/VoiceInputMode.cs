namespace Butcher.Api.Domain.Enums;

/// <summary>Comment une demande a été faite à l'assistant. Stockée en <c>snake_case</c>.</summary>
public enum VoiceInputMode
{
    /// <summary>Dictée au micro.</summary>
    Voice,

    /// <summary>Écrite au clavier (FR-005).</summary>
    Text,
}
