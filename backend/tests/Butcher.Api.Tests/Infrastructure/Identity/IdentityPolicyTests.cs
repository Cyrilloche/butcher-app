using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Tests.Infrastructure.Identity;

/// <summary>
/// Politique de mot de passe par rôle (FR-034, FR-005) : 20 caractères pour un utilisateur, 32 pour un
/// administrateur, refus énoncés en français.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class IdentityPolicyTests(PostgresDatabaseFixture fixture)
{
    private async Task<IdentityResult> ValidateAsync(AccountRole role, string password)
    {
        await using var dbContext = fixture.CreateDbContext();
        var userManager = PostgresDatabaseFixture.CreateUserManager(dbContext);
        var user = new AppUser { UserName = "jean@saloir.local", Email = "jean@saloir.local", Role = role };

        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            errors.AddRange(result.Errors);
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]);
    }

    [Theory]
    [InlineData("Jambon-Saloir-2026-Mamie")] // 24 caractères
    [InlineData("Saucisse-Curry-2026!")] // 20 caractères, pile le minimum
    public async Task User_PassphraseOfAtLeast20Chars_IsAccepted(string password)
    {
        var result = await ValidateAsync(AccountRole.User, password);

        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    [Theory]
    [InlineData("Saucisse-Curry-2026")] // 19 caractères
    [InlineData("jambon-saloir-2026-mamie")] // sans majuscule
    [InlineData("Jambon-Saloir-Mamie-Curry")] // sans chiffre
    [InlineData("JambonSaloir2026Mamie")] // sans caractère spécial
    public async Task User_PasswordNotMatchingPolicy_IsRejected(string password)
    {
        var result = await ValidateAsync(AccountRole.User, password);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Admin_PassphraseOfSixWordsWithDigit_IsAccepted()
    {
        var result = await ValidateAsync(AccountRole.Admin, "Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions");

        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    [Theory]
    [InlineData("Jambon-Saloir-2026-Mamie")] // valable pour un utilisateur, trop court pour un administrateur
    [InlineData("P@ssw0rd-P@ssw0rd-P@ssw0rd-P@ss")] // 31 caractères
    public async Task Admin_PasswordShorterThan32Chars_IsRejected(string password)
    {
        var result = await ValidateAsync(AccountRole.Admin, password);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "AdminPasswordTooShort");
    }

    [Fact]
    public async Task Rejection_IsWorded_InFrench()
    {
        var result = await ValidateAsync(AccountRole.User, "court");

        Assert.Contains(result.Errors, e => e.Description == "Le mot de passe doit compter au moins 20 caractères.");
        Assert.DoesNotContain(result.Errors, e => e.Description.StartsWith("Passwords", StringComparison.Ordinal));
    }
}
