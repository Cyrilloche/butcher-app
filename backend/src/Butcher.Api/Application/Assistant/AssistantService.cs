using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Assistant;

public interface IAssistantService
{
    Task<AssistantReply> AskTextAsync(string text, CancellationToken cancellationToken = default);

    Task<AssistantReply> AskVoiceAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Charge ce dont l'assistant a besoin (clients, catalogue, unités en stock) et confie la demande à
/// <see cref="AssistantEngine"/>. Lecture seule : un brouillon de vente s'enregistre par le formulaire.
/// </summary>
public sealed class AssistantService(AppDbContext dbContext, IMistralClient mistral, IConfiguration configuration) : IAssistantService
{
    private string ChatModel => configuration["Assistant:ChatModel"] ?? "ministral-14b-2512";

    /// <summary>Mise en phrase par le LLM : désactivée par défaut (voir <see cref="AssistantEngine"/>).</summary>
    private bool LlmSpeech => string.Equals(configuration["Assistant:LlmSpeech"], "true", StringComparison.OrdinalIgnoreCase);

    public async Task<AssistantReply> AskTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new BadRequestException("Je n'ai rien entendu.");

        var customers = await dbContext.Customers.AsNoTracking()
            .Select(c => new CustomerRef(c.Id, c.LastName, c.FirstName)).ToListAsync(cancellationToken);
        var catalog = await dbContext.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new CatalogProduct(p.Code, p.Name, p.SaleMode, p.AllowPartialSale)).ToListAsync(cancellationToken);
        var stock = await LoadStockAsync(cancellationToken);

        var (reply, _) = await new AssistantEngine(mistral, ChatModel, LlmSpeech).AskAsync(text.Trim(), customers, catalog, stock, cancellationToken);
        return reply;
    }

    public async Task<AssistantReply> AskVoiceAsync(Stream audio, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        var text = await mistral.TranscribeAsync(audio, fileName, contentType, cancellationToken);
        return await AskTextAsync(text, cancellationToken);
    }

    /// <summary>Lit une phrase de l'assistant avec la voix de Mistral. Une phrase courte : celles de l'assistant le sont.</summary>
    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 500)
            throw new BadRequestException("Phrase vide ou trop longue.");
        return mistral.SpeakAsync(text.Trim(), cancellationToken);
    }

    /// <summary>
    /// Unités <c>available</c> et <c>opened</c> des produits actifs, avec leur poids restant.
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
