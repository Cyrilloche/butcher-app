using System.Net;
using System.Text;
using Butcher.Api.Infrastructure.Mistral;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Butcher.Api.Tests.Infrastructure;

/// <summary>Client Mistral de l'assistant vocal, sans appel réseau.</summary>
public class MistralClientTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? SentBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"text":"Il me reste combien de saucissons ?"}""", Encoding.UTF8, "application/json"),
            };
        }
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
