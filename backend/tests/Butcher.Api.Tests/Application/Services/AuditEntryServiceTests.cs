using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

/// <summary>Consultation du journal : filtres, période à Paris, pagination (FR-023).</summary>
[Collection(DatabaseCollection.Name)]
public class AuditEntryServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private Guid _mireille;
    private Guid _gerard;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();

        await using var dbContext = fixture.CreateDbContext();
        _mireille = await SeedAccountAsync(dbContext, "mireille@saloir.local", "Mireille");
        _gerard = await SeedAccountAsync(dbContext, "gerard@saloir.local", "Gérard");

        // Les comptes créés ci-dessus ont leur propre entrée : on repart d'un journal vide.
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM audit_entry");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<Guid> SeedAccountAsync(Butcher.Api.Infrastructure.Data.AppDbContext dbContext, string email, string name)
    {
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = name,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private async Task SeedEntryAsync(
        Guid? accountId, AuditAction action, AuditEntityType? entityType, DateTimeOffset occurredAt, string? deletedContent = null)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.AuditEntries.Add(new AuditEntry
        {
            AccountId = accountId,
            Action = action,
            EntityType = entityType,
            EntityLabel = $"{action} {occurredAt:O}",
            OccurredAt = occurredAt,
            DeletedContent = deletedContent,
        });
        await dbContext.SaveChangesAsync();
    }

    private AuditEntryService CreateSut() => new(fixture.CreateDbContext());

    [Fact]
    public async Task SearchAsync_ReturnsMostRecentFirst_WithAuthorName()
    {
        await SeedEntryAsync(_mireille, AuditAction.Created, AuditEntityType.Sale, new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero));
        await SeedEntryAsync(_gerard, AuditAction.Updated, AuditEntityType.Sale, new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.Zero));
        await SeedEntryAsync(null, AuditAction.LoginFailed, null, new DateTimeOffset(2026, 9, 12, 8, 0, 0, TimeSpan.Zero));

        var result = await CreateSut().SearchAsync(new AuditEntryQuery());

        Assert.Equal(3, result.Total);
        Assert.Equal([AuditAction.LoginFailed, AuditAction.Updated, AuditAction.Created], result.Items.Select(i => i.Action));
        Assert.Null(result.Items[0].AccountName);
        Assert.Equal("Gérard", result.Items[1].AccountName);
    }

    [Fact]
    public async Task SearchAsync_FiltersByAuthorEntityTypeAndAction()
    {
        var at = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        await SeedEntryAsync(_mireille, AuditAction.Created, AuditEntityType.Sale, at);
        await SeedEntryAsync(_mireille, AuditAction.Deleted, AuditEntityType.ProductionBatch, at);
        await SeedEntryAsync(_gerard, AuditAction.Deleted, AuditEntityType.ProductionBatch, at);

        var service = CreateSut();

        Assert.Equal(2, (await service.SearchAsync(new AuditEntryQuery { AccountId = _mireille })).Total);
        Assert.Equal(2, (await service.SearchAsync(new AuditEntryQuery { EntityType = "production_batch" })).Total);
        Assert.Equal(1, (await service.SearchAsync(new AuditEntryQuery { AccountId = _gerard, Action = "deleted" })).Total);
    }

    [Fact]
    public async Task SearchAsync_PeriodFollowsParisDays()
    {
        // 22 h 30 UTC le 12 septembre = 0 h 30 le 13 à Paris (heure d'été, UTC+2).
        await SeedEntryAsync(_mireille, AuditAction.Created, AuditEntityType.Sale, new DateTimeOffset(2026, 9, 12, 22, 30, 0, TimeSpan.Zero));
        // 21 h 30 UTC le 12 septembre = 23 h 30 le 12 à Paris.
        await SeedEntryAsync(_mireille, AuditAction.Updated, AuditEntityType.Sale, new DateTimeOffset(2026, 9, 12, 21, 30, 0, TimeSpan.Zero));

        var day13 = new DateOnly(2026, 9, 13);
        var result = await CreateSut().SearchAsync(new AuditEntryQuery { From = day13, To = day13 });

        var item = Assert.Single(result.Items);
        Assert.Equal(AuditAction.Created, item.Action);
    }

    [Fact]
    public async Task SearchAsync_PaginatesAndCapsThePageSize()
    {
        for (var hour = 0; hour < 5; hour++)
        {
            await SeedEntryAsync(_mireille, AuditAction.Created, AuditEntityType.Customer, new DateTimeOffset(2026, 9, 10, hour, 0, 0, TimeSpan.Zero));
        }

        var service = CreateSut();
        var second = await service.SearchAsync(new AuditEntryQuery { Page = 2, PageSize = 2 });
        var capped = await service.SearchAsync(new AuditEntryQuery { PageSize = 5000 });

        Assert.Equal(5, second.Total);
        Assert.Equal(2, second.Items.Count);
        Assert.Equal(new DateTimeOffset(2026, 9, 10, 2, 0, 0, TimeSpan.Zero), second.Items[0].OccurredAt);
        Assert.Equal(AuditEntryService.MaxPageSize, capped.PageSize);
    }

    [Fact]
    public async Task SearchAsync_ReturnsTheDeletedContentAsAnObject()
    {
        await SeedEntryAsync(_mireille, AuditAction.Deleted, AuditEntityType.Customer, DateTimeOffset.UtcNow, """{"lastName":"Perrin"}""");

        var item = Assert.Single((await CreateSut().SearchAsync(new AuditEntryQuery())).Items);

        Assert.Equal("Perrin", item.DeletedContent!.Value.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task SearchAsync_RefusesUnknownFiltersAndReversedPeriods()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<BadRequestException>(() => service.SearchAsync(new AuditEntryQuery { EntityType = "ProductionBatch" }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.SearchAsync(new AuditEntryQuery { Action = "burned" }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.SearchAsync(
            new AuditEntryQuery { From = new DateOnly(2026, 9, 13), To = new DateOnly(2026, 9, 12) }));
    }
}
