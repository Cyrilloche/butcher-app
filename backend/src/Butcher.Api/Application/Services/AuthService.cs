using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class AuthService(AppDbContext dbContext, UserManager<AppUser> userManager, ITokenService tokenService)
    : IAuthService
{
    private const string InvalidCredentialsMessage = "Email ou mot de passe invalide.";
    private const string InvalidRefreshTokenMessage = "Jeton de rafraîchissement invalide ou expiré.";
    private const string DeactivatedAccountMessage = "Ce compte est désactivé.";

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Aucune ligne ne change : l'entrée est la seule trace de la tentative (FR-025).
            RecordLogin(AuditAction.LoginFailed, null, $"adresse inconnue : {email}");
            await dbContext.SaveChangesAsync();
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Verrouillage vérifié avant le mot de passe : un compte verrouillé ne dit pas si le mot de
        // passe proposé était le bon, sinon l'essai en rafale continuerait à apprendre quelque chose.
        if (await userManager.IsLockedOutAsync(user))
        {
            RecordLogin(AuditAction.LoginFailed, user, "compte verrouillé");
            await dbContext.SaveChangesAsync();
            throw LockedOut(await userManager.GetLockoutEndDateAsync(user));
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Enregistrée avec le compteur d'échecs ; le passage en verrouillage, s'il a lieu, est
            // journalisé par SaveChanges au même moment.
            RecordLogin(AuditAction.LoginFailed, user, "mot de passe erroné");
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                throw LockedOut(await userManager.GetLockoutEndDateAsync(user));
            }

            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Après le mot de passe, jamais avant : sans lui, l'état d'un compte n'a pas à être révélé.
        if (!user.IsActive)
        {
            RecordLogin(AuditAction.LoginFailed, user, "compte désactivé");
            await dbContext.SaveChangesAsync();
            throw new UnauthorizedException(DeactivatedAccountMessage);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        RecordLogin(AuditAction.LoginSucceeded, user, null);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return await IssueTokensAsync(user);
    }

    /// <summary>
    /// Ajoute au journal une tentative de connexion, enregistrée par le prochain <c>SaveChanges</c>
    /// (FR-025). L'auteur est le compte concerné : personne n'est encore connecté.
    /// </summary>
    private void RecordLogin(AuditAction action, AppUser? user, string? detail)
    {
        var name = user is null ? null : (string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName);
        var label = (name, detail) switch
        {
            (null, _) => detail,
            (_, null) => name,
            _ => $"{name} — {detail}",
        };

        dbContext.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = DateTimeOffset.UtcNow,
            AccountId = user?.Id,
            Action = action,
            EntityType = user is null ? null : AuditEntityType.Account,
            EntityId = user?.Id.ToString(),
            EntityLabel = label is { Length: > 200 } ? label[..199] + "…" : label,
        });
    }

    private static TooManyRequestsException LockedOut(DateTimeOffset? lockoutEnd)
    {
        var minutes = lockoutEnd is { } end
            ? Math.Max(1, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalMinutes))
            : 1;
        return new TooManyRequestsException(
            $"Trop de tentatives de connexion. Réessayez dans {minutes} minute{(minutes > 1 ? "s" : "")}.");
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

        // Un compte désactivé ne prolonge aucune session, et perd celles qui restaient ouvertes (FR-007).
        if (!existingToken.User!.IsActive)
        {
            await RevokeAllActiveTokensAsync(existingToken.UserId);
            throw new UnauthorizedException(DeactivatedAccountMessage);
        }

        var result = await IssueTokensAsync(existingToken.User!);

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByTokenHash = tokenService.HashRefreshToken(result.RefreshToken);
        await dbContext.SaveChangesAsync();

        return result;
    }

    public async Task<MeDto> GetAccountAsync(Guid accountId)
    {
        var user = await FindAccountOrThrowAsync(accountId);
        return new MeDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = user.Role,
        };
    }

    public async Task ChangePasswordAsync(
        Guid accountId, string currentPassword, string newPassword, string? currentRefreshTokenValue)
    {
        var user = await FindAccountOrThrowAsync(accountId);

        // Un mot de passe actuel erroné est une erreur de saisie (400), pas une session invalide (401) :
        // un 401 déclencherait côté client un rafraîchissement de session inutile.
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // Les autres appareils perdent leur session (FR-006) ; celui qui vient de changer le mot de
        // passe garde la sienne.
        var keptTokenHash = currentRefreshTokenValue is null
            ? null
            : tokenService.HashRefreshToken(currentRefreshTokenValue);
        await RevokeAllActiveTokensAsync(user.Id, exceptTokenHash: keptTokenHash);
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

    private async Task<AppUser> FindAccountOrThrowAsync(Guid accountId) =>
        await userManager.FindByIdAsync(accountId.ToString())
            ?? throw new UnauthorizedException(DeactivatedAccountMessage);

    private Task RevokeAllActiveTokensAsync(Guid userId, string? exceptTokenHash = null) =>
        RefreshTokenRevocation.RevokeAllAsync(dbContext, userId, exceptTokenHash);

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
