using Butcher.Api.Application.Assistant;
using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

public sealed record AssistantTextRequest(string Text);

public sealed record AssistantSpeechRequest(string Text);

/// <summary>
/// Assistant vocal (spike R&amp;D, docs/spike-assistant-vocal.md). Lecture seule : il répond à une
/// question de stock ou prépare un brouillon de vente, que l'utilisateur enregistre par le formulaire.
/// </summary>
[ApiController]
[Route("api/assistant")]
public class AssistantController(IAssistantService assistantService) : ControllerBase
{
    private const long MaxAudioBytes = 5 * 1024 * 1024;

    [HttpPost("text")]
    public async Task<ActionResult<AssistantReply>> AskText(AssistantTextRequest request, CancellationToken cancellationToken)
    {
        return Ok(await assistantService.AskTextAsync(request.Text, cancellationToken));
    }

    /// <summary>La phrase de réponse, lue par la voix de Mistral (MP3). Le téléphone se rabat sur sa propre voix en cas d'échec.</summary>
    [HttpPost("speech")]
    public async Task<IActionResult> Speak(AssistantSpeechRequest request, CancellationToken cancellationToken)
    {
        return File(await assistantService.SpeakAsync(request.Text, cancellationToken), "audio/mpeg");
    }

    [HttpPost("voice")]
    [RequestSizeLimit(MaxAudioBytes)]
    public async Task<ActionResult<AssistantReply>> AskVoice(IFormFile audio, CancellationToken cancellationToken)
    {
        if (audio.Length == 0)
            throw new BadRequestException("Je n'ai rien entendu.");

        await using var stream = audio.OpenReadStream();
        return Ok(await assistantService.AskVoiceAsync(stream, audio.FileName, audio.ContentType, cancellationToken));
    }
}
