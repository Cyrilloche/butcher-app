using System.Text.Json.Nodes;
using Butcher.Api.Application.Assistant;
using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>
/// L'assistant sur une vraie base (specs/006-assistant-vocal, research R-09) : le stock qu'il lit est celui
/// des écrans de stock, et chaque demande est journalisée, sans l'audio (FR-010, FR-024).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AssistantServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Mistral simulé : une transcription et un appel d'outil fixés, et la trace de ce qu'il a reçu.</summary>
    private sealed class FakeMistralClient : IMistralClient
    {
        public string Transcript { get; set; } = "";

        public List<ToolCall> ToolCalls { get; } = [];

        public Exception? ChatFailure { get; set; }

        public int ChatCalls { get; private set; }

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

    private async Task<Guid> SeedAccountAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var email = $"{Guid.NewGuid():N}@saloir.local";
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Gérard",
            Role = AccountRole.User,
            IsActive = true,
            AssistantEnabled = true,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    /// <summary>
    /// Deux jambons, dont un entamé de 500 g par une vente en tranche, et une terrine : de quoi vérifier que
    /// le poids lu est le restant, pas le poids pesé.
    /// </summary>
    private async Task SeedStockAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var jambon = new Product { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight, AllowPartialSale = true };
        var terrine = new Product { Code = "TR", Name = "Terrine", SaleMode = SaleMode.ByPiece, AllowPartialSale = false };
        dbContext.Products.AddRange(jambon, terrine);
        await dbContext.SaveChangesAsync();

        var jambonBatch = new ProductionBatch { ProductId = jambon.Id, ProductionDate = new DateOnly(2026, 9, 2), SalePrice = 28m };
        var terrineBatch = new ProductionBatch { ProductId = terrine.Id, ProductionDate = new DateOnly(2026, 9, 16), SalePrice = 7.5m };
        dbContext.ProductionBatches.AddRange(jambonBatch, terrineBatch);
        await dbContext.SaveChangesAsync();

        var units = new StockUnitService(dbContext);
        var hams = await units.AddUnitsAsync(jambonBatch.Id, new AddStockUnitsRequest { Weights = [7.000m, 8.000m] });
        await units.AddUnitsAsync(terrineBatch.Id, new AddStockUnitsRequest { Quantity = 1 });

        var customer = new Customer { LastName = "Martin" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        var sale = new Sale { SaleNumber = "V-260923-1", CustomerId = customer.Id, Date = DateTimeOffset.UtcNow };
        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();
        await new StockMovementService(dbContext).CreateAsync(hams[0].Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            SaleId = sale.Id,
            IsFullSale = false,
            SoldWeight = 0.500m,
            Amount = 14m,
        });
    }

    private (AppDbContext DbContext, AssistantService Service) CreateSut(Guid accountId, FakeMistralClient mistral)
    {
        var dbContext = fixture.CreateDbContext(new FixedCurrentAccount(accountId));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        return (dbContext, new AssistantService(dbContext, mistral, new FixedCurrentAccount(accountId), configuration,
            NullLogger<AssistantService>.Instance));
    }

    private async Task<List<VoiceRequest>> VoiceRequestsAsync()
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.VoiceRequests.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
    }

    [Fact]
    public async Task AskText_StockQuestion_ReadsTheSameStockAsTheStockScreens()
    {
        var accountId = await SeedAccountAsync();
        await SeedStockAsync();
        var mistral = new FakeMistralClient().Answers("get_stock", """{"product_code": "JB"}""");
        var (dbContext, service) = CreateSut(accountId, mistral);
        await using var _ = dbContext;

        var reply = await service.AskTextAsync("Il me reste combien de jambon ?");

        await using var reader = fixture.CreateDbContext();
        var screenUnits = (await new StockUnitService(reader).GetAllAsync(null, null, null))
            .Where(u => u.Status is StockUnitStatus.Available or StockUnitStatus.Opened && u.Weight > 1m)
            .ToList();
        var jambon = Assert.Single(reply.Stock!);
        Assert.Equal(AssistantReplyKind.StockAnswer, reply.Kind);
        Assert.Equal((1, 1), (jambon.WholeCount, jambon.OpenedCount));
        Assert.Equal(screenUnits.Sum(u => u.RemainingWeight!.Value), jambon.RemainingKg);
        Assert.Equal(14.5m, jambon.RemainingKg);
        Assert.Equal(1, mistral.ChatCalls);
    }

    [Fact]
    public async Task AskText_JournalsTheRequestWithWhatWasHeardAndTheAnswer()
    {
        var accountId = await SeedAccountAsync();
        await SeedStockAsync();
        var mistral = new FakeMistralClient().Answers("get_stock", """{"product_code": "TR"}""");
        var (dbContext, service) = CreateSut(accountId, mistral);
        await using var _ = dbContext;

        var reply = await service.AskTextAsync("  Combien de terrines ?  ");

        var request = Assert.Single(await VoiceRequestsAsync());
        Assert.Equal(request.Id, reply.RequestId);
        Assert.Equal(accountId, request.AccountId);
        Assert.Equal(VoiceInputMode.Text, request.InputMode);
        Assert.Equal("Combien de terrines ?", request.HeardText);
        Assert.Equal(VoiceRequestOutcome.StockAnswer, request.Outcome);
        Assert.Equal(reply.Speech, request.ReplySpeech);
        Assert.True(request.DurationMs >= 0);
    }

    [Fact]
    public async Task AskVoice_JournalsTheTranscriptNeverTheAudio()
    {
        var accountId = await SeedAccountAsync();
        await SeedStockAsync();
        var mistral = new FakeMistralClient { Transcript = "Vends une terrine à madame Martin." }
            .Answers("draft_sale", """{"customer_token": "[CLIENT_1]", "lines": [{"product_code": "TR", "quantity": 1}]}""");
        var (dbContext, service) = CreateSut(accountId, mistral);
        await using var _ = dbContext;
        var audio = "RIFF-audio-du-telephone"u8.ToArray();

        var reply = await service.AskVoiceAsync(new MemoryStream(audio), "demande.webm", "audio/webm");

        var request = Assert.Single(await VoiceRequestsAsync());
        Assert.Equal(AssistantReplyKind.SaleDraft, reply.Kind);
        Assert.Equal(VoiceRequestOutcome.SaleDraft, request.Outcome);
        Assert.Equal(VoiceInputMode.Voice, request.InputMode);
        Assert.Equal("Vends une terrine à madame Martin.", request.HeardText);
        Assert.DoesNotContain("RIFF", request.HeardText + request.ReplySpeech);
    }

    [Fact]
    public async Task AskVoice_NothingHeard_AnswersSo_WithoutCallingTheLanguageModel()
    {
        var accountId = await SeedAccountAsync();
        var mistral = new FakeMistralClient { Transcript = "   " };
        var (dbContext, service) = CreateSut(accountId, mistral);
        await using var _ = dbContext;

        var reply = await service.AskVoiceAsync(new MemoryStream([1, 2, 3]), "demande.webm", "audio/webm");

        Assert.Equal(AssistantReplyKind.NotUnderstood, reply.Kind);
        Assert.Equal(AssistantService.NothingHeardSpeech, reply.Speech);
        Assert.Equal(0, mistral.ChatCalls);
        var request = Assert.Single(await VoiceRequestsAsync());
        Assert.Null(request.HeardText);
        Assert.Equal(VoiceRequestOutcome.NotUnderstood, request.Outcome);
    }

    [Fact]
    public async Task AskText_ServiceUnavailable_JournalsAnError_AndStillFails()
    {
        var accountId = await SeedAccountAsync();
        var mistral = new FakeMistralClient { ChatFailure = new ServiceUnavailableException("L'assistant vocal ne répond pas pour le moment.") };
        var (dbContext, service) = CreateSut(accountId, mistral);
        await using var _ = dbContext;

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => service.AskTextAsync("Il reste du jambon ?"));

        var request = Assert.Single(await VoiceRequestsAsync());
        Assert.Equal(VoiceRequestOutcome.Error, request.Outcome);
        Assert.Equal("Il reste du jambon ?", request.HeardText);
        Assert.Null(request.ReplySpeech);
    }

    [Fact]
    public async Task AskText_Blank_IsRefused_AndNotJournaled()
    {
        var accountId = await SeedAccountAsync();
        var (dbContext, service) = CreateSut(accountId, new FakeMistralClient());
        await using var _ = dbContext;

        await Assert.ThrowsAsync<BadRequestException>(() => service.AskTextAsync("  "));

        Assert.Empty(await VoiceRequestsAsync());
    }

    [Fact]
    public async Task AskText_IsNotAGesture_NothingAddedToTheAuditTrail()
    {
        var accountId = await SeedAccountAsync();
        await SeedStockAsync();
        int AuditCount() { using var reader = fixture.CreateDbContext(); return reader.AuditEntries.Count(); }
        var before = AuditCount();
        var (dbContext, service) = CreateSut(accountId, new FakeMistralClient().Answers("get_stock", "{}"));
        await using var _ = dbContext;

        await service.AskTextAsync("Qu'est-ce qu'il me reste ?");

        Assert.Equal(before, AuditCount());
    }
}
