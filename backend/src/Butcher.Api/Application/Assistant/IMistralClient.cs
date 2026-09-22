using System.Text.Json.Nodes;

namespace Butcher.Api.Application.Assistant;

public sealed record ToolCall(string Id, string Name, string Arguments);

/// <param name="Message">Le message de l'assistant tel que reçu, à renvoyer tel quel dans la suite de la conversation.</param>
public sealed record ChatResult(string? Content, IReadOnlyList<ToolCall> ToolCalls, int PromptTokens, int CompletionTokens, JsonObject Message);

/// <summary>
/// Accès à La Plateforme de Mistral (spike assistant vocal, cadrage D-04, D-07) : seul le backend
/// l'appelle, jamais le frontend. Ne reçoit que du texte pseudonymisé et le catalogue.
/// </summary>
public interface IMistralClient
{
    /// <param name="toolChoice"><c>any</c> pour imposer un outil, <c>none</c> pour une réponse en texte.</param>
    Task<ChatResult> ChatAsync(string model, JsonArray messages, JsonArray? tools, string toolChoice, CancellationToken cancellationToken = default);

    Task<string> TranscribeAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken = default);
}
