using System.Diagnostics;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Assistant;

public interface IAssistantService
{
    Task<AssistantReply> AskTextAsync(string text, CancellationToken cancellationToken = default);

    Task<AssistantReply> AskVoiceAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>La phrase de réponse d'une demande du compte connecté, lue par la voix de Mistral (MP3).</summary>
    Task<byte[]> SpeakReplyAsync(long requestId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reçoit une demande à l'assistant vocal (RF-34, RF-35), charge ce dont il a besoin (clients, catalogue,
/// unités en stock), la confie à <see cref="AssistantEngine"/> et la journalise (RF-36). Lecture seule :
/// un brouillon de vente s'enregistre par le formulaire (FR-009).
/// </summary>
/// <remarks>
/// Seul écrivain de <see cref="VoiceRequest"/> : une ligne par demande, quelle qu'en soit l'issue, jamais
/// l'audio (FR-024, specs/006-assistant-vocal research R-02).
/// </remarks>
public sealed class AssistantService(
    AppDbContext dbContext,
    IMistralClient mistral,
    ICurrentAccount currentAccount,
    IConfiguration configuration,
    ILogger<AssistantService> logger) : IAssistantService
{
    public const string NothingHeardSpeech = "Je n'ai rien entendu. Réessaie en parlant près du téléphone.";

    /// <summary>Au-delà, la voix d'une réponse ne se demande plus : la réponse a été lue ou abandonnée.</summary>
    public static readonly TimeSpan SpeechWindow = TimeSpan.FromMinutes(10);

    public const string RateLimitedMessage = "Tu as fait beaucoup de demandes : réessaie dans quelques minutes.";

    private string ChatModel => configuration["Assistant:ChatModel"] ?? "ministral-14b-2512";

    /// <summary>Demandes admises par compte sur l'heure glissante (FR-023) ; 30 par défaut.</summary>
    private int MaxRequestsPerHour => int.TryParse(configuration["Assistant:MaxRequestsPerHour"], out var max) && max > 0 ? max : 30;

    public Task<AssistantReply> AskTextAsync(string text, CancellationToken cancellationToken = default)
    {
        // Une demande écrite vide n'est pas une demande : refusée sans être journalisée.
        if (string.IsNullOrWhiteSpace(text))
            throw new BadRequestException("Écris ta demande.");

        return HandleAsync(VoiceInputMode.Text, _ => Task.FromResult(text), cancellationToken);
    }

    public Task<AssistantReply> AskVoiceAsync(Stream audio, string fileName, string contentType,
        CancellationToken cancellationToken = default) =>
        HandleAsync(VoiceInputMode.Voice,
            token => mistral.TranscribeAsync(audio, fileName, contentType, token), cancellationToken);

    /// <summary>
    /// Seule la phrase de réponse journalisée d'une demande du même compte, récente, part au service de
    /// synthèse : aucun texte ne peut lui être passé (FR-020, FR-021 ; research R-05).
    /// </summary>
    public async Task<byte[]> SpeakReplyAsync(long requestId, CancellationToken cancellationToken = default)
    {
        var accountId = currentAccount.AccountId ?? throw new UnauthorizedException("Compte non identifié.");
        var since = DateTimeOffset.UtcNow - SpeechWindow;
        var speech = await dbContext.VoiceRequests.AsNoTracking()
            .Where(r => r.Id == requestId && r.AccountId == accountId && r.OccurredAt >= since && r.ReplySpeech != null)
            .Select(r => r.ReplySpeech)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Cette réponse n'est plus disponible.");

        return await mistral.SpeakAsync(speech, cancellationToken);
    }

    /// <summary>
    /// Entend la demande (transcription ou texte tel quel), la comprend, et journalise le résultat. Une
    /// erreur est journalisée avec l'issue <c>error</c> puis repart vers l'appelant (FR-027).
    /// </summary>
    private async Task<AssistantReply> HandleAsync(VoiceInputMode inputMode,
        Func<CancellationToken, Task<string>> hear, CancellationToken cancellationToken)
    {
        var accountId = currentAccount.AccountId ?? throw new UnauthorizedException("Compte non identifié.");
        var occurredAt = DateTimeOffset.UtcNow;
        var started = Stopwatch.GetTimestamp();
        await EnsureWithinLimitAsync(accountId, inputMode, occurredAt, cancellationToken);
        string? heard = null;

        try
        {
            heard = (await hear(cancellationToken)).Trim();
            var reply = heard.Length == 0
                ? new AssistantReply(AssistantReplyKind.NotUnderstood, NothingHeardSpeech, "", null, null)
                : await UnderstandAsync(heard, cancellationToken);

            var request = await RecordAsync(new VoiceRequest
            {
                AccountId = accountId,
                OccurredAt = occurredAt,
                InputMode = inputMode,
                HeardText = heard.Length == 0 ? null : heard,
                Outcome = OutcomeOf(reply.Kind),
                ReplySpeech = reply.Speech,
                DurationMs = ElapsedMs(started),
            }, cancellationToken);
            return reply with { RequestId = request.Id };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordErrorAsync(new VoiceRequest
            {
                AccountId = accountId,
                OccurredAt = occurredAt,
                InputMode = inputMode,
                HeardText = string.IsNullOrEmpty(heard) ? null : heard,
                Outcome = VoiceRequestOutcome.Error,
                DurationMs = ElapsedMs(started),
            });
            throw;
        }
    }

    /// <summary>
    /// Refuse la demande de trop avant tout appel extérieur, transcription comprise, et la journalise
    /// (FR-023 ; research R-04). Les refus ne comptent pas : la limite se relâche d'elle-même.
    /// </summary>
    private async Task EnsureWithinLimitAsync(Guid accountId, VoiceInputMode inputMode, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var since = now - TimeSpan.FromHours(1);
        var count = await dbContext.VoiceRequests.CountAsync(r =>
            r.AccountId == accountId && r.OccurredAt > since && r.Outcome != VoiceRequestOutcome.RateLimited,
            cancellationToken);
        if (count < MaxRequestsPerHour)
            return;

        await RecordAsync(new VoiceRequest
        {
            AccountId = accountId,
            OccurredAt = now,
            InputMode = inputMode,
            Outcome = VoiceRequestOutcome.RateLimited,
            DurationMs = 0,
        }, cancellationToken);
        throw new TooManyRequestsException(RateLimitedMessage);
    }

    private async Task<AssistantReply> UnderstandAsync(string heard, CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers.AsNoTracking()
            .Select(c => new CustomerRef(c.Id, c.LastName, c.FirstName)).ToListAsync(cancellationToken);
        var catalog = await dbContext.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new CatalogProduct(p.Code, p.Name, p.SaleMode, p.AllowPartialSale)).ToListAsync(cancellationToken);
        var stock = await LoadStockAsync(cancellationToken);

        var (reply, _) = await new AssistantEngine(mistral, ChatModel).AskAsync(heard, customers, catalog, stock, cancellationToken);
        return reply;
    }

    private async Task<VoiceRequest> RecordAsync(VoiceRequest request, CancellationToken cancellationToken)
    {
        dbContext.VoiceRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>
    /// Journalise une demande en erreur sans masquer l'erreur d'origine : si l'écriture échoue à son tour,
    /// elle est seulement tracée dans les logs.
    /// </summary>
    private async Task RecordErrorAsync(VoiceRequest request)
    {
        try
        {
            // Une écriture précédente a pu échouer et laisser des entités suivies : on repart propre.
            dbContext.ChangeTracker.Clear();
            await RecordAsync(request, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Demande à l'assistant en erreur non journalisée.");
        }
    }

    private static VoiceRequestOutcome OutcomeOf(AssistantReplyKind kind) => kind switch
    {
        AssistantReplyKind.StockAnswer => VoiceRequestOutcome.StockAnswer,
        AssistantReplyKind.SaleDraft => VoiceRequestOutcome.SaleDraft,
        _ => VoiceRequestOutcome.NotUnderstood,
    };

    private static int ElapsedMs(long started) => (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    /// <summary>
    /// Unités <c>available</c> et <c>opened</c> des produits actifs, avec leur poids restant (FR-010).
    /// Même agrégat que <c>StockUnitService.GetAllAsync</c> : la somme des ventes, écrite en ligne pour
    /// qu'EF Core la traduise en SQL.
    /// </summary>
    private async Task<List<SellableUnit>> LoadStockAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.StockUnits.AsNoTracking()
            .Where(u => (u.Status == StockUnitStatus.Available || u.Status == StockUnitStatus.Opened) && u.Batch!.Product!.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                Unit = u,
                u.Batch!.Product!.Code,
                u.Batch.Product.Name,
                u.Batch.Product.SaleMode,
                u.Batch.Product.AllowPartialSale,
                u.Batch.ProductionDate,
                u.Batch.SalePrice,
                SoldWeight = u.StockMovements.Where(m => m.Type == MovementType.Sale).Sum(m => m.SoldWeight ?? 0m),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SellableUnit(r.Unit.Id, r.Unit.UnitNumber, r.Code, r.Name, r.SaleMode,
            r.AllowPartialSale, r.Unit.BatchId, r.ProductionDate, r.SalePrice, r.Unit.Weight,
            Services.StockMovementRules.ComputeRemainingWeight(r.Unit, r.SoldWeight), r.Unit.Status)).ToList();
    }
}
