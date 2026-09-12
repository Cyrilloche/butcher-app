using System.Security.Claims;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Common.Authorization;

/// <summary>
/// Les droits sont relus en base à chaque requête (ADR-011, research R-02 et R-03).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AccountAuthorizationHandlerTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedAccountAsync(AccountRole role, bool isActive = true)
    {
        await using var dbContext = fixture.CreateDbContext();
        var email = $"{Guid.NewGuid():N}@saloir.local";
        var account = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Compte de test",
            Role = role,
            IsActive = isActive,
        };
        dbContext.AppUsers.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private static ClaimsPrincipal PrincipalFor(Guid accountId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId.ToString())], authenticationType: "Test"));

    private async Task<AuthorizationHandlerContext> AuthorizeAsync(ClaimsPrincipal principal, AccountRequirement requirement)
    {
        await using var dbContext = fixture.CreateDbContext();
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        await new AccountAuthorizationHandler(dbContext).HandleAsync(context);
        return context;
    }

    private static IEnumerable<string> FailureReasons(AuthorizationHandlerContext context) =>
        context.FailureReasons.Select(r => r.Message);

    [Theory]
    [InlineData(AccountRole.User)]
    [InlineData(AccountRole.Admin)]
    public async Task ActiveRequirement_ActiveAccount_Succeeds(AccountRole role)
    {
        var accountId = await SeedAccountAsync(role);

        var context = await AuthorizeAsync(PrincipalFor(accountId), AccountRequirement.Active);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ActiveRequirement_DeactivatedAccount_FailsAsInactive()
    {
        var accountId = await SeedAccountAsync(AccountRole.Admin, isActive: false);

        var context = await AuthorizeAsync(PrincipalFor(accountId), AccountRequirement.Active);

        Assert.False(context.HasSucceeded);
        Assert.Contains(AccountAuthorizationHandler.InactiveAccountReason, FailureReasons(context));
    }

    [Fact]
    public async Task ActiveRequirement_UnknownAccount_FailsAsInactive()
    {
        var context = await AuthorizeAsync(PrincipalFor(Guid.NewGuid()), AccountRequirement.Active);

        Assert.False(context.HasSucceeded);
        Assert.Contains(AccountAuthorizationHandler.InactiveAccountReason, FailureReasons(context));
    }

    [Fact]
    public async Task AdministratorRequirement_User_FailsAsAdminRequired()
    {
        var accountId = await SeedAccountAsync(AccountRole.User);

        var context = await AuthorizeAsync(PrincipalFor(accountId), AccountRequirement.Administrator);

        Assert.False(context.HasSucceeded);
        Assert.Contains(AccountAuthorizationHandler.AdminRequiredReason, FailureReasons(context));
    }

    [Fact]
    public async Task AdministratorRequirement_Admin_Succeeds()
    {
        var accountId = await SeedAccountAsync(AccountRole.Admin);

        var context = await AuthorizeAsync(PrincipalFor(accountId), AccountRequirement.Administrator);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task AdministratorRequirement_DemotedAdmin_FailsOnNextRequest()
    {
        var accountId = await SeedAccountAsync(AccountRole.Admin);
        var principal = PrincipalFor(accountId);
        Assert.True((await AuthorizeAsync(principal, AccountRequirement.Administrator)).HasSucceeded);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var account = await dbContext.AppUsers.SingleAsync(u => u.Id == accountId);
            account.Role = AccountRole.User;
            await dbContext.SaveChangesAsync();
        }

        // Même jeton, rôle changé en base : le refus est immédiat.
        var context = await AuthorizeAsync(principal, AccountRequirement.Administrator);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AnyRequirement_AnonymousPrincipal_DoesNotSucceed()
    {
        var context = await AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), AccountRequirement.Active);

        Assert.False(context.HasSucceeded);
        Assert.Empty(FailureReasons(context));
    }
}
