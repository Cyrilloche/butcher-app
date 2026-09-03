using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class AuthServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private const string Email = "jean@saloir.local";
    private const string Password = "Correct-Password-123!";

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
