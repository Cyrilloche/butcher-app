using System.Data;
using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class AccountService(AppDbContext dbContext, UserManager<AppUser> userManager) : IAccountService
{
    private const string LastActiveAdminMessage = "L'outil doit toujours garder au moins un administrateur actif.";

    // Codes d'erreur Identity qui signalent un conflit avec un compte existant, et non une saisie invalide.
    private static readonly HashSet<string> DuplicateErrorCodes =
        [nameof(IdentityErrorDescriber.DuplicateEmail), nameof(IdentityErrorDescriber.DuplicateUserName)];

    public async Task<List<AccountDto>> GetAllAsync()
    {
        var accounts = await dbContext.AppUsers
            .AsNoTracking()
            .OrderByDescending(u => u.IsActive)
            .ThenBy(u => u.DisplayName)
            .ToListAsync();

        return accounts.Select(ToDto).ToList();
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request)
    {
        var account = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = RequireDisplayName(request.DisplayName),
            Role = request.Role,
        };

        ThrowIfFailed(await userManager.CreateAsync(account, request.Password));

        return ToDto(account);
    }

    public async Task<AccountDto> UpdateAsync(Guid accountId, UpdateAccountRequest request)
    {
        // Sérialisable : deux rétrogradations simultanées ne doivent pas pouvoir retirer chacune « l'autre »
        // administrateur et laisser l'outil sans aucun (FR-008).
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var account = await FindOrThrowAsync(accountId);
        var displayName = RequireDisplayName(request.DisplayName);
        var isPromotion = account.Role == AccountRole.User && request.Role == AccountRole.Admin;
        var isDemotion = account.Role == AccountRole.Admin && request.Role == AccountRole.User;

        if (isDemotion && account.IsActive)
        {
            await EnsureAnotherActiveAdminAsync(account.Id);
        }

        if (isPromotion && string.IsNullOrEmpty(request.NewPassword))
        {
            throw new BadRequestException(
                "Promouvoir un compte administrateur exige de lui choisir un nouveau mot de passe.");
        }

        account.DisplayName = displayName;
        account.Role = request.Role;

        if (isPromotion)
        {
            // Le rôle cible est posé avant la validation : c'est la règle de l'administrateur qui s'applique
            // (FR-035). En cas de refus, la transaction n'est pas validée et rien n'est enregistré.
            await ReplacePasswordAsync(account, request.NewPassword!);
            await RefreshTokenRevocation.RevokeAllAsync(dbContext, account.Id);
        }
        else
        {
            ThrowIfFailed(await userManager.UpdateAsync(account));
        }

        await transaction.CommitAsync();
        return ToDto(account);
    }

    public async Task DeactivateAsync(Guid accountId, Guid actingAccountId)
    {
        if (accountId == actingAccountId)
        {
            throw new ConflictException("Vous ne pouvez pas désactiver votre propre compte.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var account = await FindOrThrowAsync(accountId);
        if (!account.IsActive)
        {
            return;
        }

        if (account.Role == AccountRole.Admin)
        {
            await EnsureAnotherActiveAdminAsync(account.Id);
        }

        account.IsActive = false;
        ThrowIfFailed(await userManager.UpdateAsync(account));
        await RefreshTokenRevocation.RevokeAllAsync(dbContext, account.Id);

        await transaction.CommitAsync();
    }

    public async Task ReactivateAsync(Guid accountId)
    {
        var account = await FindOrThrowAsync(accountId);
        if (account.IsActive)
        {
            return;
        }

        account.IsActive = true;
        ThrowIfFailed(await userManager.UpdateAsync(account));
    }

    public async Task ResetPasswordAsync(Guid accountId, string newPassword)
    {
        var account = await FindOrThrowAsync(accountId);

        await ReplacePasswordAsync(account, newPassword);
        await userManager.ResetAccessFailedCountAsync(account);
        await userManager.SetLockoutEndDateAsync(account, null);
        await RefreshTokenRevocation.RevokeAllAsync(dbContext, account.Id);
    }

    /// <summary>
    /// Valide le mot de passe contre la politique du rôle porté par le compte, puis le remplace.
    /// Renouveler le jeton de sécurité enregistre le compte entier, rôle compris, en une écriture.
    /// </summary>
    private async Task ReplacePasswordAsync(AppUser account, string newPassword)
    {
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            errors.AddRange((await validator.ValidateAsync(userManager, account, newPassword)).Errors);
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException(string.Join(" ", errors.Select(e => e.Description)));
        }

        account.PasswordHash = userManager.PasswordHasher.HashPassword(account, newPassword);
        ThrowIfFailed(await userManager.UpdateSecurityStampAsync(account));
    }

    private async Task EnsureAnotherActiveAdminAsync(Guid excludedAccountId)
    {
        var anotherActiveAdmin = await dbContext.AppUsers.AnyAsync(u =>
            u.Id != excludedAccountId && u.IsActive && u.Role == AccountRole.Admin);

        if (!anotherActiveAdmin)
        {
            throw new ConflictException(LastActiveAdminMessage);
        }
    }

    private async Task<AppUser> FindOrThrowAsync(Guid accountId) =>
        await dbContext.AppUsers.SingleOrDefaultAsync(u => u.Id == accountId)
            ?? throw new NotFoundException("Compte introuvable.");

    private static string RequireDisplayName(string displayName) =>
        string.IsNullOrWhiteSpace(displayName)
            ? throw new BadRequestException("Le nom affiché est obligatoire.")
            : displayName.Trim();

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(e => e.Description));
        if (result.Errors.Any(e => DuplicateErrorCodes.Contains(e.Code)))
        {
            throw new ConflictException(message);
        }

        throw new BadRequestException(message);
    }

    private static AccountDto ToDto(AppUser account) => new()
    {
        Id = account.Id,
        Email = account.Email ?? string.Empty,
        DisplayName = account.DisplayName,
        Role = account.Role,
        IsActive = account.IsActive,
        LastLoginAt = account.LastLoginAt,
        CreatedAt = account.CreatedAt,
    };
}
