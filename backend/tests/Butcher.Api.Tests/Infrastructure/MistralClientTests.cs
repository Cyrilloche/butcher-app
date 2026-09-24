using System.Net;
using System.Text;
using Butcher.Api.Infrastructure.Mistral;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Butcher.Api.Tests.Infrastructure;

/// <summary>Client Mistral de l'assistant vocal, sans appel réseau.</summary>
public class MistralClientTests
{
    private sealed class RecordingHandler(string response = """{"text":"Il me reste combien de saucissons ?"}""") : HttpMessageHandler
    {
        public string? SentBody { get; private set; }

        public string? SentPath { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            SentPath = request.RequestUri!.AbsolutePath;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static MistralClient Client(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri(MistralClient.BaseAddress) },
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MISTRAL_API_KEY"] = "test" }).Build(),
        NullLogger<MistralClient>.Instance);

    [Fact]
    public async Task Speak_SendsOnlyTheSentence_WithAFrenchVoice_AndDecodesTheMp3()
    {
        var handler = new RecordingHandler("""{"audio_data":"SUQz"}""");

        var audio = await Client(handler).SpeakAsync("Saucisse curry : il t'en reste 11.");

        Assert.Equal("ID3"u8.ToArray(), audio);
        Assert.Equal("/v1/audio/speech", handler.SentPath);
        Assert.Contains("\"voice\":\"fr_marie_neutral\"", handler.SentBody);
        Assert.Contains("Saucisse curry", handler.SentBody);
    }

    [Fact]
    public async Task Speak_BlankVoiceSetting_FallsBackToTheFrenchVoice()
    {
        // Docker Compose passe une variable non renseignée comme une chaîne vide, pas comme une absence.
        var handler = new RecordingHandler("""{"audio_data":"SUQz"}""");
        var client = new MistralClient(
            new HttpClient(handler) { BaseAddress = new Uri(MistralClient.BaseAddress) },
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MISTRAL_API_KEY"] = "test",
                ["Assistant:SpeechVoice"] = "",
                ["Assistant:SpeechModel"] = "  ",
            }).Build(),
            NullLogger<MistralClient>.Instance);

        await client.SpeakAsync("Terrine : il t'en reste 1.");

        Assert.Contains("\"voice\":\"fr_marie_neutral\"", handler.SentBody);
        Assert.Contains("\"model\":\"voxtral-mini-tts-2603\"", handler.SentBody);
    }

    [Theory]
    [InlineData("audio/webm;codecs=opus")]
    [InlineData("audio/webm")]
    [InlineData("")]
    public async Task Transcribe_AcceptsTheTypeChromeSends(string contentType)
    {
        var handler = new RecordingHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MISTRAL_API_KEY"] = "test" }).Build();
        var client = new MistralClient(new HttpClient(handler) { BaseAddress = new Uri(MistralClient.BaseAddress) },
            configuration, NullLogger<MistralClient>.Instance);

        var text = await client.TranscribeAsync(new MemoryStream([1, 2, 3]), "demande.webm", contentType);

        Assert.Equal("Il me reste combien de saucissons ?", text);
        Assert.Contains("Content-Type: audio/webm", handler.SentBody);
    }
}
