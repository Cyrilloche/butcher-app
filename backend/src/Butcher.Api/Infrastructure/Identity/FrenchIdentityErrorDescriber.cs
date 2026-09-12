using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Infrastructure.Identity;

/// <summary>
/// Messages d'Identity en français, pour ceux qui peuvent remonter jusqu'à l'écran : règles de mot de
/// passe (FR-005) et email déjà utilisé. Les utilisateurs ne voient jamais d'anglais (CLAUDE.md §6).
/// </summary>
public sealed class FrenchIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        Describe(nameof(PasswordTooShort), $"Le mot de passe doit compter au moins {length} caractères.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Describe(nameof(PasswordRequiresUniqueChars), $"Le mot de passe doit utiliser au moins {uniqueChars} caractères différents.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Describe(nameof(PasswordRequiresNonAlphanumeric), "Le mot de passe doit contenir au moins un caractère spécial, par exemple un tiret.");

    public override IdentityError PasswordRequiresDigit() =>
        Describe(nameof(PasswordRequiresDigit), "Le mot de passe doit contenir au moins un chiffre.");

    public override IdentityError PasswordRequiresLower() =>
        Describe(nameof(PasswordRequiresLower), "Le mot de passe doit contenir au moins une minuscule.");

    public override IdentityError PasswordRequiresUpper() =>
        Describe(nameof(PasswordRequiresUpper), "Le mot de passe doit contenir au moins une majuscule.");

    public override IdentityError PasswordMismatch() =>
        Describe(nameof(PasswordMismatch), "Le mot de passe actuel est incorrect.");

    public override IdentityError DuplicateEmail(string email) =>
        Describe(nameof(DuplicateEmail), $"L'adresse {email} est déjà utilisée par un autre compte.");

    public override IdentityError DuplicateUserName(string userName) =>
        Describe(nameof(DuplicateUserName), $"L'adresse {userName} est déjà utilisée par un autre compte.");

    public override IdentityError InvalidEmail(string? email) =>
        Describe(nameof(InvalidEmail), $"L'adresse « {email} » n'est pas valide.");

    private static IdentityError Describe(string code, string description) =>
        new() { Code = code, Description = description };
}
