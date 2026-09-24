using System.Text.Json.Nodes;
using Butcher.Api.Application.Assistant;

namespace Butcher.Api.Tests.Support;

/// <summary>
/// Mistral simulé pour les tests de l'assistant : une transcription et des appels d'outil fixés, et la
/// trace de ce qu'il a reçu (ce que le LLM voit, les phrases lues).
/// </summary>
public sealed class FakeMistralClient : IMistralClient
{
    public string Transcript { get; set; } = "";

    public List<ToolCall> ToolCalls { get; } = [];

    public Exception? ChatFailure { get; set; }

    public int ChatCalls { get; private set; }

    /// <summary>Tous les messages envoyés au LLM, système compris, tels qu'il les a reçus.</summary>
    public List<string> SentMessages { get; } = [];

    public List<string> SpokenTexts { get; } = [];

    public FakeMistralClient Answers(string tool, string arguments)
    {
        ToolCalls.Add(new ToolCall($"call-{ToolCalls.Count}", tool, arguments));
        return this;
    }

    public Task<ChatResult> ChatAsync(string model, JsonArray messages, JsonArray? tools, string toolChoice,
        CancellationToken cancellationToken = default)
    {
        ChatCalls++;
        SentMessages.AddRange(messages.Select(m => (string?)m?["content"] ?? ""));
        if (ChatFailure is not null)
            throw ChatFailure;
        return Task.FromResult(new ChatResult(null, ToolCalls, 100, 10, new JsonObject()));
    }

    public Task<string> TranscribeAsync(Stream audio, string fileName, string contentType,
        CancellationToken cancellationToken = default) => Task.FromResult(Transcript);

    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        SpokenTexts.Add(text);
        return Task.FromResult("ID3"u8.ToArray());
    }
}
