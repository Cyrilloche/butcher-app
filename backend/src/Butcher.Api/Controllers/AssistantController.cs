using Butcher.Api.Application.Assistant;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Controllers;

public sealed record AssistantTextRequest(string Text);

/// <summary>
/// Assistant vocal (spike R&amp;D, docs/spike-assistant-vocal.md). Lecture seule : il répond à une
/// question de stock ou prépare un brouillon de vente, que l'utilisateur enregistre par le formulaire.
/// </summary>
[ApiController]
[Route("api/assistant")]
[Authorize(Policy = AuthorizationPolicies.AssistantEnabled)]
public class AssistantController(IAssistantService assistantService) : ControllerBase
{
    private const long MaxAudioBytes = 5 * 1024 * 1024;

    [HttpPost("text")]
    public async Task<ActionResult<AssistantReply>> AskText(AssistantTextRequest request, CancellationToken cancellationToken)
    {
        return Ok(await assistantService.AskTextAsync(request.Text, cancellationToken));
    }

    /// <summary>
    /// La réponse d'une demande du compte, lue par la voix de Mistral (MP3). Le téléphone se rabat sur sa
    /// propre voix en cas d'échec (FR-008).
    /// </summary>
    [HttpGet("requests/{id:long}/speech")]
    public async Task<IActionResult> Speak(long id, CancellationToken cancellationToken)
    {
        return File(await assistantService.SpeakReplyAsync(id, cancellationToken), "audio/mpeg");
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
