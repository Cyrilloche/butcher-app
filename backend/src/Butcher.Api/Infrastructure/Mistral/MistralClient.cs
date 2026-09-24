using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Butcher.Api.Application.Assistant;
using Butcher.Api.Common;
using Butcher.Api.Common.Exceptions;

namespace Butcher.Api.Infrastructure.Mistral;

/// <summary>
/// Client HTTP de La Plateforme (api.mistral.ai), sans SDK : deux routes suffisent.
/// Une limite de débit passagère (429) est retentée ; un modèle fermé au compte
/// (limite à zéro requête par minute) échoue tout de suite, avec un message qui le dit.
/// </summary>
public sealed class MistralClient(HttpClient http, IConfiguration configuration, ILogger<MistralClient> logger) : IMistralClient
{
    public const string BaseAddress = "https://api.mistral.ai/v1/";
    private const int MaxAttempts = 4;

    private string TranscriptionModel => configuration.ValueOr("Assistant:TranscriptionModel", "voxtral-mini-2602");

    private string SpeechModel => configuration.ValueOr("Assistant:SpeechModel", "voxtral-mini-tts-2603");

    /// <summary>Voix préréglée de Mistral ; les voix françaises sont « fr_marie_* » (neutral, happy, curious…).</summary>
    private string SpeechVoice => configuration.ValueOr("Assistant:SpeechVoice", "fr_marie_neutral");

    public async Task<ChatResult> ChatAsync(string model, JsonArray messages, JsonArray? tools, string toolChoice,
        CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["model"] = model, ["messages"] = messages.DeepClone(), ["temperature"] = 0 };
        if (tools is not null)
        {
            body["tools"] = tools.DeepClone();
            body["tool_choice"] = toolChoice;
        }

        var json = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        }, cancellationToken);

        var message = json["choices"]![0]!["message"]!.AsObject();
        var toolCalls = (message["tool_calls"] as JsonArray ?? [])
            .Select(c => new ToolCall(
                (string)c!["id"]!,
                (string)c["function"]!["name"]!,
                c["function"]!["arguments"] is JsonValue v ? v.ToString() : c["function"]!["arguments"]!.ToJsonString()))
            .ToList();
        return new ChatResult(
            (string?)message["content"],
            toolCalls,
            (int?)json["usage"]?["prompt_tokens"] ?? 0,
            (int?)json["usage"]?["completion_tokens"] ?? 0,
            (JsonObject)message.DeepClone());
    }

    public async Task<string> TranscribeAsync(Stream audio, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var json = await SendAsync(() =>
        {
            var file = new ByteArrayContent(bytes);
            // Chrome envoie « audio/webm;codecs=opus » : le constructeur refuse les paramètres, Parse les garde.
            file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var type)
                ? type
                : new MediaTypeHeaderValue("audio/webm");
            return new HttpRequestMessage(HttpMethod.Post, "audio/transcriptions")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent(TranscriptionModel), "model" },
                    { new StringContent("fr"), "language" },
                    { file, "file", fileName },
                },
            };
        }, cancellationToken);
        return (string?)json["text"] ?? "";
    }

    public async Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["model"] = SpeechModel,
            ["input"] = text,
            ["voice"] = SpeechVoice,
            ["response_format"] = "mp3",
        };
        var json = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "audio/speech")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        }, cancellationToken);
        return Convert.FromBase64String((string?)json["audio_data"] ?? "");
    }

    private async Task<JsonNode> SendAsync(Func<HttpRequestMessage> request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["MISTRAL_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ServiceUnavailableException("L'assistant vocal n'est pas configuré (clé Mistral absente).");

        for (var attempt = 1; ; attempt++)
        {
            using var message = request();
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await http.SendAsync(message, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return JsonNode.Parse(content)!;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var limit = response.Headers.TryGetValues("x-ratelimit-limit-req-minute", out var values) ? values.FirstOrDefault() : null;
                if (limit == "0")
                    throw new ServiceUnavailableException("Ce modèle Mistral n'est pas ouvert sur le compte (limite à zéro requête).");
                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
                    continue;
                }
            }

            logger.LogWarning("Mistral a répondu {Status} : {Body}", (int)response.StatusCode, content);
            throw new ServiceUnavailableException($"L'assistant vocal ne répond pas pour le moment (Mistral {(int)response.StatusCode}).");
        }
    }
}
