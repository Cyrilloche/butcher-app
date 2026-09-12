using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

/// <summary>
/// Fermeture des sessions d'un compte, partagée par l'authentification et la gestion des comptes.
/// </summary>
internal static class RefreshTokenRevocation
{
    /// <summary>
    /// Révoque tous les refresh tokens encore actifs du compte, sauf celui dont le hash est fourni.
    /// </summary>
    public static async Task RevokeAllAsync(AppDbContext dbContext, Guid accountId, string? exceptTokenHash = null)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == accountId && t.RevokedAt == null && t.TokenHash != exceptTokenHash)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync();
    }
}
