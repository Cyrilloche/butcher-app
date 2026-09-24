using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

/// <summary>
/// Gestion des comptes (ADR-011) : FR-003, FR-008 (dernier administrateur actif), FR-009, FR-035.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AccountServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    private const string AdminPassword = "Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions";
    private const string UserPassword = "Jambon-Saloir-2026-Mamie";

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private (AppDbContext DbContext, UserManager<AppUser> UserManager, AccountService Service) CreateSut()
    {
        var dbContext = fixture.CreateDbContext();
        var userManager = PostgresDatabaseFixture.CreateUserManager(dbContext);
        return (dbContext, userManager, new AccountService(dbContext, userManager));
    }

    private static async Task<AppUser> SeedAccountAsync(
        UserManager<AppUser> userManager, string email, AccountRole role, bool isActive = true)
    {
        var account = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = email.Split('@')[0],
            Role = role,
            IsActive = isActive,
        };
        var result = await userManager.CreateAsync(
            account, role == AccountRole.Admin ? AdminPassword : UserPassword);
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
        return account;
    }

    private async Task<AppUser> ReloadAsync(Guid accountId)
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.AppUsers.AsNoTracking().SingleAsync(u => u.Id == accountId);
    }

    private async Task SeedActiveRefreshTokenAsync(Guid accountId)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = accountId,
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await dbContext.SaveChangesAsync();
    }

    private async Task<int> CountActiveRefreshTokensAsync(Guid accountId)
    {
        await using var reader = fixture.CreateDbContext();
        return await reader.RefreshTokens.CountAsync(t => t.UserId == accountId && t.RevokedAt == null);
    }

    // --- Liste et création -------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsActiveAndDeactivatedAccounts()
    {
        var (_, userManager, service) = CreateSut();
        await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);
        await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User, isActive: false);

        var accounts = await service.GetAllAsync();

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.Email == "mireille@saloir.local" && !a.IsActive);
    }

    [Fact]
    public async Task CreateAsync_User_CreatesActiveAccountAbleToLogIn()
    {
        var (_, userManager, service) = CreateSut();

        var created = await service.CreateAsync(new CreateAccountRequest
        {
            Email = "mireille@saloir.local",
            DisplayName = "  Mireille ",
            Role = AccountRole.User,
            Password = UserPassword,
        });

        Assert.Equal("Mireille", created.DisplayName);
        Assert.Equal(AccountRole.User, created.Role);
        Assert.True(created.IsActive);
        var stored = await userManager.FindByEmailAsync("mireille@saloir.local");
        Assert.True(await userManager.CheckPasswordAsync(stored!, UserPassword));
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyUsedByDeactivatedAccount_IsConflict()
    {
        var (_, userManager, service) = CreateSut();
        await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User, isActive: false);

        var error = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateAccountRequest
        {
            Email = "mireille@saloir.local",
            DisplayName = "Mireille",
            Password = UserPassword,
        }));

        Assert.Equal("L'adresse mireille@saloir.local est déjà utilisée par un autre compte.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_AdminWithUserLengthPassword_IsRefusedWithRuleInFrench()
    {
        var (_, _, service) = CreateSut();

        var error = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateAccountRequest
        {
            Email = "admin@saloir.local",
            DisplayName = "Administrateur",
            Role = AccountRole.Admin,
            Password = UserPassword,
        }));

        Assert.Contains("au moins 32 caractères", error.Message);
    }

    [Fact]
    public async Task CreateAsync_BlankDisplayName_IsRefused()
    {
        var (_, _, service) = CreateSut();

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateAccountRequest
        {
            Email = "mireille@saloir.local",
            DisplayName = "   ",
            Password = UserPassword,
        }));
    }

    // --- Modification et rôle -----------------------------------------------

    [Fact]
    public async Task UpdateAsync_DisplayName_IsSaved()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);

        await service.UpdateAsync(account.Id, new UpdateAccountRequest { DisplayName = "Maman", Role = AccountRole.User });

        Assert.Equal("Maman", (await ReloadAsync(account.Id)).DisplayName);
    }

    // --- Assistant vocal (RF-36, FR-026) ------------------------------------

    [Fact]
    public async Task CreateAsync_NewAccount_HasNoAssistant()
    {
        var (_, _, service) = CreateSut();

        var created = await service.CreateAsync(new CreateAccountRequest
        {
            Email = "mireille@saloir.local",
            DisplayName = "Mireille",
            Role = AccountRole.User,
            Password = UserPassword,
        });

        Assert.False(created.AssistantEnabled);
        Assert.False((await ReloadAsync(created.Id)).AssistantEnabled);
    }

    [Fact]
    public async Task UpdateAsync_AssistantEnabled_TurnsTheAssistantOnThenOff()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "gerard@saloir.local", AccountRole.User);

        var enabled = await service.UpdateAsync(account.Id,
            new UpdateAccountRequest { DisplayName = "Gérard", Role = AccountRole.User, AssistantEnabled = true });
        Assert.True(enabled.AssistantEnabled);
        Assert.True((await ReloadAsync(account.Id)).AssistantEnabled);

        await service.UpdateAsync(account.Id,
            new UpdateAccountRequest { DisplayName = "Gérard", Role = AccountRole.User, AssistantEnabled = false });
        Assert.False((await ReloadAsync(account.Id)).AssistantEnabled);
    }

    [Fact]
    public async Task UpdateAsync_AssistantEnabledAbsent_KeepsTheCurrentValue()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "gerard@saloir.local", AccountRole.User);
        await service.UpdateAsync(account.Id,
            new UpdateAccountRequest { DisplayName = "Gérard", Role = AccountRole.User, AssistantEnabled = true });

        // Un renommage ne doit pas couper l'assistant du compte.
        await service.UpdateAsync(account.Id, new UpdateAccountRequest { DisplayName = "Gégé", Role = AccountRole.User });

        Assert.True((await ReloadAsync(account.Id)).AssistantEnabled);
    }

    [Fact]
    public async Task UpdateAsync_PromotionWithoutNewPassword_IsRefused()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);

        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(
            account.Id, new UpdateAccountRequest { DisplayName = "Mireille", Role = AccountRole.Admin }));

        Assert.Equal(AccountRole.User, (await ReloadAsync(account.Id)).Role);
    }

    [Fact]
    public async Task UpdateAsync_PromotionWithUserLengthPassword_IsRefusedAndNothingIsSaved()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);

        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(account.Id, new UpdateAccountRequest
        {
            DisplayName = "Nouveau nom",
            Role = AccountRole.Admin,
            NewPassword = "Saucisse-Curry-2026-Terrine",
        }));

        var stored = await ReloadAsync(account.Id);
        Assert.Equal(AccountRole.User, stored.Role);
        Assert.Equal("mireille", stored.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_PromotionWithAdminPassword_PromotesReplacesPasswordAndClosesSessions()
    {
        var (_, userManager, service) = CreateSut();
        var account = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);
        await SeedActiveRefreshTokenAsync(account.Id);
        const string newAdminPassword = "Tundra-Pickle7-Harbor-Velvet-Gazebo-Lantern";

        var updated = await service.UpdateAsync(account.Id, new UpdateAccountRequest
        {
            DisplayName = "Mireille",
            Role = AccountRole.Admin,
            NewPassword = newAdminPassword,
        });

        Assert.Equal(AccountRole.Admin, updated.Role);
        var (_, checker, _) = CreateSut();
        var stored = await checker.FindByIdAsync(account.Id.ToString());
        Assert.Equal(AccountRole.Admin, stored!.Role);
        Assert.True(await checker.CheckPasswordAsync(stored, newAdminPassword));
        Assert.False(await checker.CheckPasswordAsync(stored, UserPassword));
        Assert.Equal(0, await CountActiveRefreshTokensAsync(account.Id));
    }

    [Fact]
    public async Task UpdateAsync_DemotingLastActiveAdmin_IsConflict()
    {
        var (_, userManager, service) = CreateSut();
        var admin = await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);
        await SeedAccountAsync(userManager, "ancien@saloir.local", AccountRole.Admin, isActive: false);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            admin.Id, new UpdateAccountRequest { DisplayName = "admin", Role = AccountRole.User }));

        Assert.Equal(AccountRole.Admin, (await ReloadAsync(admin.Id)).Role);
    }

    [Fact]
    public async Task UpdateAsync_DemotingAdminWhenAnotherIsActive_Succeeds()
    {
        var (_, userManager, service) = CreateSut();
        var admin = await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);
        await SeedAccountAsync(userManager, "cyril@saloir.local", AccountRole.Admin);

        await service.UpdateAsync(admin.Id, new UpdateAccountRequest { DisplayName = "admin", Role = AccountRole.User });

        Assert.Equal(AccountRole.User, (await ReloadAsync(admin.Id)).Role);
    }

    // --- Désactivation, réactivation, réinitialisation ------------------------

    [Fact]
    public async Task DeactivateAsync_OwnAccount_IsConflict()
    {
        var (_, userManager, service) = CreateSut();
        var admin = await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);
        await SeedAccountAsync(userManager, "cyril@saloir.local", AccountRole.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(admin.Id, actingAccountId: admin.Id));

        Assert.True((await ReloadAsync(admin.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_LastActiveAdmin_IsConflict()
    {
        var (_, userManager, service) = CreateSut();
        var admin = await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(admin.Id, actingAccountId: Guid.NewGuid()));

        Assert.True((await ReloadAsync(admin.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_User_DeactivatesAndClosesSessions()
    {
        var (_, userManager, service) = CreateSut();
        var admin = await SeedAccountAsync(userManager, "admin@saloir.local", AccountRole.Admin);
        var user = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);
        await SeedActiveRefreshTokenAsync(user.Id);

        await service.DeactivateAsync(user.Id, actingAccountId: admin.Id);

        Assert.False((await ReloadAsync(user.Id)).IsActive);
        Assert.Equal(0, await CountActiveRefreshTokensAsync(user.Id));
    }

    [Fact]
    public async Task ReactivateAsync_DeactivatedAccount_IsActiveAgain()
    {
        var (_, userManager, service) = CreateSut();
        var user = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User, isActive: false);

        await service.ReactivateAsync(user.Id);

        Assert.True((await ReloadAsync(user.Id)).IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReplacesPasswordUnlocksAndClosesSessions()
    {
        var (_, userManager, service) = CreateSut();
        var user = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
        await SeedActiveRefreshTokenAsync(user.Id);
        const string newPassword = "Saucisse-Curry-2026-Terrine";

        await service.ResetPasswordAsync(user.Id, newPassword);

        var (_, checker, _) = CreateSut();
        var stored = await checker.FindByIdAsync(user.Id.ToString());
        Assert.True(await checker.CheckPasswordAsync(stored!, newPassword));
        Assert.False(await checker.IsLockedOutAsync(stored!));
        Assert.Equal(0, await CountActiveRefreshTokensAsync(user.Id));
    }

    [Fact]
    public async Task ResetPasswordAsync_NonCompliantPassword_IsRefusedAndKeepsOldPassword()
    {
        var (_, userManager, service) = CreateSut();
        var user = await SeedAccountAsync(userManager, "mireille@saloir.local", AccountRole.User);

        await Assert.ThrowsAsync<BadRequestException>(() => service.ResetPasswordAsync(user.Id, "Court-1!"));

        var (_, checker, _) = CreateSut();
        Assert.True(await checker.CheckPasswordAsync((await checker.FindByIdAsync(user.Id.ToString()))!, UserPassword));
    }

    [Fact]
    public async Task AnyOperation_UnknownAccount_IsNotFound()
    {
        var (_, _, service) = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReactivateAsync(Guid.NewGuid()));
    }
}
