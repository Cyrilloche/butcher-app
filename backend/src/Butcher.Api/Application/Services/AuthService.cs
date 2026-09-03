using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class AuthService(AppDbContext dbContext, UserManager<AppUser> userManager, ITokenService tokenService)
    : IAuthService
{
    private const string InvalidCredentialsMessage = "Email ou mot de passe invalide.";
    private const string InvalidRefreshTokenMessage = "Jeton de rafraîchissement invalide ou expiré.";

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResult> RefreshAsync(string refreshTokenValue)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshTokenValue);
        var existingToken = await dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash)
            ?? throw new UnauthorizedException(InvalidRefreshTokenMessage);

        if (existingToken.RevokedAt is not null)
        {
            // Un token déjà révoqué qui revient est le signe classique d'un vol/rejeu :
            // on révoque tout ce qui est encore actif pour cet utilisateur par précaution.
            await RevokeAllActiveTokensAsync(existingToken.UserId);
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        if (existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        var result = await IssueTokensAsync(existingToken.User!);

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByTokenHash = tokenService.HashRefreshToken(result.RefreshToken);
        await dbContext.SaveChangesAsync();

        return result;
    }

    public async Task LogoutAsync(string refreshTokenValue)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshTokenValue);
        var existingToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (existingToken is not null && existingToken.RevokedAt is null)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<AuthResult> IssueTokensAsync(AppUser user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshTokenValue = tokenService.GenerateRefreshTokenValue();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.Add(tokenService.RefreshTokenLifetime);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshTokenValue),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        return new AuthResult(accessToken.Value, accessToken.ExpiresAtUtc, refreshTokenValue, refreshTokenExpiresAt);
    }
}
