using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Infrastructure.Identity;

/// <summary>
/// Durcit la politique de mot de passe pour un administrateur (FR-034, research R-05). Les options
/// Identity sont globales et ignorent le rôle ; un validateur reçoit le compte et peut le lire.
/// </summary>
/// <remarks>
/// Le rôle lu est celui porté par l'objet au moment de la validation : pour une promotion, l'appelant
/// pose le rôle cible avant de valider le nouveau mot de passe (FR-035).
/// </remarks>
public sealed class AdminPasswordValidator : IPasswordValidator<AppUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (user.Role != AccountRole.Admin || password is null)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var errors = new List<IdentityError>();

        if (password.Length < IdentityPolicy.AdminMinPasswordLength)
        {
            errors.Add(new IdentityError
            {
                Code = "AdminPasswordTooShort",
                Description =
                    $"Un mot de passe d'administrateur doit compter au moins {IdentityPolicy.AdminMinPasswordLength} caractères.",
            });
        }

        if (password.Distinct().Count() < IdentityPolicy.AdminMinUniqueChars)
        {
            errors.Add(new IdentityError
            {
                Code = "AdminPasswordRequiresUniqueChars",
                Description =
                    $"Un mot de passe d'administrateur doit utiliser au moins {IdentityPolicy.AdminMinUniqueChars} caractères différents.",
            });
        }

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]));
    }
}
