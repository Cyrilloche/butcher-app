using Butcher.Api.Application.Services;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

/// <summary>
/// Usage de l'assistant vocal (RF-36, FR-025) : par compte et par semaine de Paris, délai médian, détail des
/// demandes avec la phrase entendue.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AssistantReportTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateOnly From = new(2026, 9, 1), To = new(2026, 9, 30);

    // Dimanche 20 septembre, 23 h 30 à Paris (UTC+2) : semaine du lundi 14.
    private static readonly DateTimeOffset SundayLateInParis = new(2026, 9, 20, 21, 30, 0, TimeSpan.Zero);

    // Lundi 21 septembre, 0 h 30 à Paris, encore dimanche en UTC : semaine du lundi 21.
    private static readonly DateTimeOffset MondayEarlyInParis = new(2026, 9, 20, 22, 30, 0, TimeSpan.Zero);

    private Guid _gerard;
    private Guid _mireille;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _gerard = await SeedAccountAsync("Gérard");
        _mireille = await SeedAccountAsync("Mireille");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedAccountAsync(string name)
    {
        await using var dbContext = fixture.CreateDbContext();
        var email = $"{Guid.NewGuid():N}@saloir.local";
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = name,
            Role = AccountRole.User,
            IsActive = true,
            AssistantEnabled = true,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private async Task SeedAsync(Guid accountId, DateTimeOffset at, VoiceRequestOutcome outcome, int durationMs,
        string? heard = "Il reste du jambon ?")
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.VoiceRequests.Add(new VoiceRequest
        {
            AccountId = accountId,
            OccurredAt = at,
            InputMode = VoiceInputMode.Voice,
            HeardText = heard,
            Outcome = outcome,
            ReplySpeech = outcome == VoiceRequestOutcome.RateLimited ? null : "Jambon : il t'en reste 2.",
            DurationMs = durationMs,
        });
        await dbContext.SaveChangesAsync();
    }

    private ReportService CreateSut() => new(fixture.CreateDbContext());

    [Fact]
    public async Task Usage_GroupsByAccountAndByParisWeek()
    {
        await SeedAsync(_gerard, SundayLateInParis, VoiceRequestOutcome.StockAnswer, 1000);
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.SaleDraft, 1500);
        await SeedAsync(_mireille, MondayEarlyInParis, VoiceRequestOutcome.NotUnderstood, 900);

        var usage = await CreateSut().GetAssistantUsageAsync(From, To);

        Assert.Equal(
            [("Gérard", new DateOnly(2026, 9, 21)), ("Mireille", new DateOnly(2026, 9, 21)), ("Gérard", new DateOnly(2026, 9, 14))],
            usage.Select(u => (u.AccountName, u.WeekStart)));
    }

    [Fact]
    public async Task Usage_CountsEachOutcome_AndTheMedianIgnoresRefusedRequests()
    {
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.StockAnswer, 1000);
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.SaleDraft, 2000);
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.Error, 5000);
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.RateLimited, 0, heard: null);

        var week = Assert.Single(await CreateSut().GetAssistantUsageAsync(From, To));

        Assert.Equal((4, 1, 1, 0, 1, 1), (week.Requests, week.StockAnswers, week.SaleDrafts, week.NotUnderstood, week.Errors, week.RateLimited));
        Assert.Equal(2000, week.MedianDurationMs);
    }

    [Fact]
    public async Task Requests_MostRecentFirst_WithWhatWasHeard_FilteredByAccount()
    {
        await SeedAsync(_gerard, SundayLateInParis, VoiceRequestOutcome.StockAnswer, 1000, heard: "Combien de terrines ?");
        await SeedAsync(_gerard, MondayEarlyInParis, VoiceRequestOutcome.NotUnderstood, 900, heard: "Vent de saucisson.");
        await SeedAsync(_mireille, MondayEarlyInParis, VoiceRequestOutcome.SaleDraft, 1200);

        var requests = await CreateSut().GetAssistantRequestsAsync(From, To, _gerard, limit: 50);

        Assert.Equal(["Vent de saucisson.", "Combien de terrines ?"], requests.Select(r => r.HeardText));
        Assert.All(requests, r => Assert.Equal("Gérard", r.AccountName));
    }

    [Fact]
    public async Task Requests_AreLimited_AndOutsideThePeriodExcluded()
    {
        await SeedAsync(_gerard, new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero), VoiceRequestOutcome.StockAnswer, 1000);
        for (var i = 0; i < 3; i++)
        {
            await SeedAsync(_gerard, MondayEarlyInParis.AddMinutes(i), VoiceRequestOutcome.StockAnswer, 1000);
        }

        var requests = await CreateSut().GetAssistantRequestsAsync(From, To, null, limit: 2);

        Assert.Equal(2, requests.Count);
        Assert.All(requests, r => Assert.True(r.OccurredAt >= MondayEarlyInParis.AddMinutes(1)));
    }
}
