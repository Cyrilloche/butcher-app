namespace Butcher.Api.Domain.Enums;

/// <summary>Issue d'une demande à l'assistant vocal (RF-36, FR-024). Stockée en <c>snake_case</c>.</summary>
public enum VoiceRequestOutcome
{
    /// <summary>Réponse à une question de stock.</summary>
    StockAnswer,

    /// <summary>Brouillon de vente préparé, avec ou sans réponse de stock dans la même phrase.</summary>
    SaleDraft,

    /// <summary>Ni stock ni vente, ou demande inexploitable.</summary>
    NotUnderstood,

    /// <summary>Service extérieur indisponible, transcription impossible, erreur interne.</summary>
    Error,

    /// <summary>Refusée par la limite du compte, sans appel extérieur (FR-023).</summary>
    RateLimited,
}
