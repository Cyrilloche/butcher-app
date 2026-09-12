using Butcher.Api.Domain.Entities;
using Butcher.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Tests.Infrastructure.Identity;

[Collection(DatabaseCollection.Name)]
public class IdentityPolicyTests(PostgresDatabaseFixture fixture)
{
    private async Task<IdentityResult> ValidateAsync(string password)
    {
        await using var dbContext = fixture.CreateDbContext();
        var userManager = PostgresDatabaseFixture.CreateUserManager(dbContext);
        var user = new AppUser { UserName = "jean@saloir.local", Email = "jean@saloir.local" };

        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return IdentityResult.Success;
    }

    [Fact]
    public async Task Passphrase_OfSixWordsWithDigit_IsAccepted()
    {
        var result = await ValidateAsync("Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Correct-Password-123!")] // l'ancien standard : complexe mais trop court
    [InlineData("P@ssw0rd-P@ssw0rd-P@ssw0rd-P@ss")] // 31 caractères, un de moins que le minimum
    [InlineData("finlike-scorer4-wildfire-grazing-unbiased-sessions")] // sans majuscule
    public async Task Password_NotMatchingPolicy_IsRejected(string password)
    {
        var result = await ValidateAsync(password);

        Assert.False(result.Succeeded);
    }
}
