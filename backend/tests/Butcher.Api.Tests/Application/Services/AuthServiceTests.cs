using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Infrastructure.Identity;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class AuthServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private const string Email = "jean@saloir.local";
    private const string Password = "Correct-Horse4-Battery-Staple-Saloir";

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<AppUser> SeedUserAsync(UserManager<AppUser> userManager)
    {
        var user = new AppUser { UserName = Email, Email = Email, CreatedAt = DateTimeOffset.UtcNow };
        var result = await userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static (AppDbContext DbContext, UserManager<AppUser> UserManager, AuthService Service) CreateSut(
        PostgresDatabaseFixture fixture)
    {
        var dbContext = fixture.CreateDbContext();
        var userManager = PostgresDatabaseFixture.CreateUserManager(dbContext);
        var tokenService = new TokenService(PostgresDatabaseFixture.CreateJwtConfiguration());
        var service = new AuthService(dbContext, userManager, tokenService);
        return (dbContext, userManager, service);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        var result = await service.LoginAsync(Email, Password);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        var storedTokenCount = await dbContext.RefreshTokens.CountAsync(t => t.UserId == user.Id);
        Assert.Equal(1, storedTokenCount);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsUnauthorizedException()
    {
        var (_, _, service) = CreateSut(fixture);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync("unknown@saloir.local", Password));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var (_, userManager, service) = CreateSut(fixture);
        await SeedUserAsync(userManager);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));
    }

    [Fact]
    public async Task LoginAsync_AfterTooManyWrongPasswords_LocksAccountEvenWithCorrectPassword()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        for (var attempt = 1; attempt < IdentityPolicy.MaxFailedAccessAttempts; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));
        }

        // La tentative qui atteint le seuil verrouille, et le dit.
        await Assert.ThrowsAsync<TooManyRequestsException>(() => service.LoginAsync(Email, "wrong-password"));
        Assert.True(await userManager.IsLockedOutAsync(user));

        // Le bon mot de passe ne passe plus tant que le verrou tient.
        await Assert.ThrowsAsync<TooManyRequestsException>(() => service.LoginAsync(Email, Password));
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ResetsFailedAttempts()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        for (var attempt = 1; attempt < IdentityPolicy.MaxFailedAccessAttempts; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));
        }

        await service.LoginAsync(Email, Password);

        Assert.Equal(0, await userManager.GetAccessFailedCountAsync(user));
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));
    }

    private static async Task DeactivateAsync(UserManager<AppUser> userManager, AppUser user)
    {
        user.IsActive = false;
        await userManager.UpdateAsync(user);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_RecordsLastLogin()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        var before = DateTimeOffset.UtcNow;

        await service.LoginAsync(Email, Password);

        var stored = await dbContext.AppUsers.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.NotNull(stored.LastLoginAt);
        Assert.True(stored.LastLoginAt >= before);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccountWithCorrectPassword_IsRefusedAsDeactivated()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        await DeactivateAsync(userManager, user);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, Password));

        Assert.Equal("Ce compte est désactivé.", error.Message);
        Assert.Equal(0, await dbContext.RefreshTokens.CountAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccountWithWrongPassword_DoesNotRevealDeactivation()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        await DeactivateAsync(userManager, user);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));

        Assert.Equal("Email ou mot de passe invalide.", error.Message);
    }

    [Fact]
    public async Task RefreshAsync_DeactivatedAccount_IsRefusedAndRevokesAllSessions()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        var firstSession = await service.LoginAsync(Email, Password);
        await service.LoginAsync(Email, Password);
        await DeactivateAsync(userManager, user);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync(firstSession.RefreshToken));

        Assert.Equal("Ce compte est désactivé.", error.Message);
        Assert.Equal(0, await dbContext.RefreshTokens.CountAsync(t => t.UserId == user.Id && t.RevokedAt == null));
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsDisplayNameAndRole()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        user.DisplayName = "Mireille";
        user.Role = AccountRole.Admin;
        await userManager.UpdateAsync(user);

        var me = await service.GetAccountAsync(user.Id);

        Assert.Equal(user.Id, me.Id);
        Assert.Equal(Email, me.Email);
        Assert.Equal("Mireille", me.DisplayName);
        Assert.Equal(AccountRole.Admin, me.Role);
        Assert.False(me.AssistantEnabled);
    }

    [Fact]
    public async Task GetAccountAsync_ReportsTheAssistantOnceEnabled()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        user.AssistantEnabled = true;
        await userManager.UpdateAsync(user);

        var me = await service.GetAccountAsync(user.Id);

        Assert.True(me.AssistantEnabled);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ReplacesPasswordAndKeepsOnlyCurrentSession()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        var otherDevice = await service.LoginAsync(Email, Password);
        var thisDevice = await service.LoginAsync(Email, Password);
        const string newPassword = "Saucisse-Curry-2026-Terrine";

        await service.ChangePasswordAsync(user.Id, Password, newPassword, thisDevice.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, Password));
        await service.LoginAsync(Email, newPassword);

        var tokenService = new TokenService(PostgresDatabaseFixture.CreateJwtConfiguration());
        var otherHash = tokenService.HashRefreshToken(otherDevice.RefreshToken);
        var thisHash = tokenService.HashRefreshToken(thisDevice.RefreshToken);
        Assert.NotNull((await dbContext.RefreshTokens.AsNoTracking().SingleAsync(t => t.TokenHash == otherHash)).RevokedAt);
        Assert.Null((await dbContext.RefreshTokens.AsNoTracking().SingleAsync(t => t.TokenHash == thisHash)).RevokedAt);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_IsRefusedAsInputError()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        var error = await Assert.ThrowsAsync<BadRequestException>(
            () => service.ChangePasswordAsync(user.Id, "wrong-password", "Saucisse-Curry-2026-Terrine", null));

        Assert.Equal("Le mot de passe actuel est incorrect.", error.Message);
        await service.LoginAsync(Email, Password);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonCompliantNewPassword_StatesRuleInFrench()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        var error = await Assert.ThrowsAsync<BadRequestException>(
            () => service.ChangePasswordAsync(user.Id, Password, "Court-1!", null));

        Assert.Contains("au moins 20 caractères", error.Message);
        await service.LoginAsync(Email, Password);
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesAndRevokesOldToken()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        await SeedUserAsync(userManager);
        var loginResult = await service.LoginAsync(Email, Password);

        var refreshResult = await service.RefreshAsync(loginResult.RefreshToken);

        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
        Assert.NotEqual(loginResult.AccessToken, refreshResult.AccessToken);

        var tokenService = new TokenService(PostgresDatabaseFixture.CreateJwtConfiguration());
        var oldTokenHash = tokenService.HashRefreshToken(loginResult.RefreshToken);
        var oldToken = await dbContext.RefreshTokens.FirstAsync(t => t.TokenHash == oldTokenHash);
        Assert.NotNull(oldToken.RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_WithAlreadyRevokedToken_RevokesAllActiveTokensAndThrows()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        var loginResult = await service.LoginAsync(Email, Password);
        await service.RefreshAsync(loginResult.RefreshToken); // rotates -> loginResult.RefreshToken becomes revoked

        // Rejeu du token déjà révoqué : doit échouer ET révoquer tout le reste (détection de vol).
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync(loginResult.RefreshToken));

        var stillActive = await dbContext.RefreshTokens.CountAsync(t => t.UserId == user.Id && t.RevokedAt == null);
        Assert.Equal(0, stillActive);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ThrowsUnauthorizedException()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        var tokenService = new TokenService(PostgresDatabaseFixture.CreateJwtConfiguration());
        var rawValue = tokenService.GenerateRefreshTokenValue();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(rawValue),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-31),
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync(rawValue));
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_ThrowsUnauthorizedException()
    {
        var (_, _, service) = CreateSut(fixture);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync("not-a-real-token"));
    }

    // --- Journal des connexions (FR-025) -------------------------------------------------------------

    private async Task<List<AuditEntry>> LoginEntriesAsync()
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.AuditEntries
            .Where(e => e.Action == AuditAction.LoginSucceeded || e.Action == AuditAction.LoginFailed || e.Action == AuditAction.LockedOut)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_RecordsTheLoginUnderTheAccount()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        await service.LoginAsync(Email, Password);

        var entry = Assert.Single(await LoginEntriesAsync());
        Assert.Equal(AuditAction.LoginSucceeded, entry.Action);
        Assert.Equal(user.Id, entry.AccountId);
        Assert.Equal(AuditEntityType.Account, entry.EntityType);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_RecordsAFailure()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, "wrong-password"));

        var entry = Assert.Single(await LoginEntriesAsync());
        Assert.Equal(AuditAction.LoginFailed, entry.Action);
        Assert.Equal(user.Id, entry.AccountId);
        Assert.Equal($"{Email} — mot de passe erroné", entry.EntityLabel);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_RecordsTheTypedAddressWithoutAuthor()
    {
        var (_, _, service) = CreateSut(fixture);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync("inconnu@saloir.local", Password));

        var entry = Assert.Single(await LoginEntriesAsync());
        Assert.Equal(AuditAction.LoginFailed, entry.Action);
        Assert.Null(entry.AccountId);
        Assert.Null(entry.EntityType);
        Assert.Equal("adresse inconnue : inconnu@saloir.local", entry.EntityLabel);
    }

    [Fact]
    public async Task LoginAsync_ReachingTheLockout_RecordsItOnce_ThenRecordsRefusedAttempts()
    {
        var (_, userManager, service) = CreateSut(fixture);
        await SeedUserAsync(userManager);

        for (var attempt = 1; attempt <= IdentityPolicy.MaxFailedAccessAttempts; attempt++)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => service.LoginAsync(Email, "wrong-password"));
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => service.LoginAsync(Email, Password));

        var entries = await LoginEntriesAsync();
        Assert.Single(entries, e => e.Action == AuditAction.LockedOut);
        Assert.Equal(IdentityPolicy.MaxFailedAccessAttempts + 1, entries.Count(e => e.Action == AuditAction.LoginFailed));
        Assert.Equal($"{Email} — compte verrouillé", entries[^1].EntityLabel);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_RecordsTheRefusal()
    {
        var (_, userManager, service) = CreateSut(fixture);
        var user = await SeedUserAsync(userManager);
        await DeactivateAsync(userManager, user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(Email, Password));

        var entry = Assert.Single(await LoginEntriesAsync());
        Assert.Equal($"{Email} — compte désactivé", entry.EntityLabel);
    }

    [Fact]
    public async Task LogoutAsync_RevokesToken()
    {
        var (dbContext, userManager, service) = CreateSut(fixture);
        await SeedUserAsync(userManager);
        var loginResult = await service.LoginAsync(Email, Password);

        await service.LogoutAsync(loginResult.RefreshToken);

        var tokenService = new TokenService(PostgresDatabaseFixture.CreateJwtConfiguration());
        var tokenHash = tokenService.HashRefreshToken(loginResult.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstAsync(t => t.TokenHash == tokenHash);
        Assert.NotNull(token.RevokedAt);
    }
}
