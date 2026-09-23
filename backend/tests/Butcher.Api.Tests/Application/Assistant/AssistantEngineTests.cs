using Butcher.Api.Application.Assistant;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>
/// La chaîne de l'assistant avec un LLM simulé (RF-34, RF-35) : ce que le backend fait de l'outil que le LLM
/// choisit, et ce que le LLM reçoit.
/// </summary>
public class AssistantEngineTests
{
    private const int Martin = 1, Martine = 2, Gerard = 3;

    private static readonly CustomerRef[] Customers =
    [
        new(Martin, "Martin", null),
        new(Martine, "Roux", "Martine"),
        new(Gerard, "Gérard", null),
    ];

    private static readonly CatalogProduct[] Catalog =
    [
        new("JB", "Jambon", SaleMode.ByWeight, true),
        new("SC", "Saucisson", SaleMode.ByWeight, false),
        new("TR", "Terrine", SaleMode.ByPiece, false),
    ];

    private static readonly DateOnly Sep2 = new(2026, 9, 2), Sep16 = new(2026, 9, 16);

    private static readonly SellableUnit[] Stock =
    [
        new(1, "SC-260902-1", "SC", "Saucisson", SaleMode.ByWeight, false, 1, Sep2, 24m, 0.300m, 0.300m, StockUnitStatus.Available),
        new(2, "SC-260916-1", "SC", "Saucisson", SaleMode.ByWeight, false, 2, Sep16, 26m, 0.320m, 0.320m, StockUnitStatus.Available),
        new(10, "JB-260902-1", "JB", "Jambon", SaleMode.ByWeight, true, 3, Sep2, 28m, 8m, 4.1m, StockUnitStatus.Opened),
        new(11, "JB-260916-1", "JB", "Jambon", SaleMode.ByWeight, true, 4, Sep16, 28m, 7m, 7m, StockUnitStatus.Available),
        new(20, "TR-260916-1", "TR", "Terrine", SaleMode.ByPiece, false, 5, Sep16, 7.5m, null, null, StockUnitStatus.Available),
    ];

    private static Task<(AssistantReply Reply, AssistantTrace Trace)> AskAsync(FakeMistralClient mistral, string text) =>
        new AssistantEngine(mistral, "ministral-14b-2512").AskAsync(text, Customers, Catalog, Stock);

    // --- Question de stock (US1) --------------------------------------------------------------

    [Fact]
    public async Task GetStock_OneProduct_AnswersWithTheServerSentenceAndTheDetail()
    {
        var mistral = new FakeMistralClient().Answers("get_stock", """{"product_code": "SC"}""");

        var (reply, _) = await AskAsync(mistral, "Il me reste combien de saucissons ?");

        Assert.Equal(AssistantReplyKind.StockAnswer, reply.Kind);
        var saucisson = Assert.Single(reply.Stock!);
        Assert.Equal(2, saucisson.Batches.Count);
        Assert.Equal(StockSummaryBuilder.Speech(reply.Stock!, "Saucisson"), reply.Speech);
        Assert.Equal("Il me reste combien de saucissons ?", reply.Heard);
    }

    [Fact]
    public async Task GetStock_WithoutProduct_CoversTheWholeStock()
    {
        var mistral = new FakeMistralClient().Answers("get_stock", "{}");

        var (reply, _) = await AskAsync(mistral, "Qu'est-ce qu'il me reste en stock ?");

        Assert.Equal(["JB", "SC", "TR"], reply.Stock!.Select(p => p.Code).Order());
    }

    [Fact]
    public async Task GetStock_CodeOutsideTheCatalogue_NeverGivesAnotherProduct()
    {
        var mistral = new FakeMistralClient().Answers("get_stock", """{"product_code": "CH"}""");

        var (reply, _) = await AskAsync(mistral, "Il reste du chorizo ?");

        Assert.Null(reply.Stock);
        Assert.Equal(AssistantEngine.UnknownProductSpeech, reply.Speech);
    }

    [Fact]
    public async Task NotUnderstood_RemindsWhatCanBeAsked()
    {
        var mistral = new FakeMistralClient().Answers("not_understood", """{"reason": "hors sujet"}""");

        var (reply, _) = await AskAsync(mistral, "Mets des chansons pour enfants.");

        Assert.Equal(AssistantReplyKind.NotUnderstood, reply.Kind);
        Assert.Equal(AssistantEngine.NotUnderstoodSpeech, reply.Speech);
        Assert.Null(reply.Stock);
        Assert.Null(reply.Draft);
    }

    [Fact]
    public async Task EveryRequest_CallsTheLanguageModelOnce_TheSentenceIsNeverItsOwn()
    {
        var mistral = new FakeMistralClient().Answers("get_stock", """{"product_code": "JB"}""");

        var (reply, _) = await AskAsync(mistral, "Combien de jambon ?");

        Assert.Equal(1, mistral.ChatCalls);
        Assert.StartsWith("Jambon : il t'en reste 1 entier et 1 entamé", reply.Speech);
    }
}
