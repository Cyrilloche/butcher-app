using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Infrastructure.Identity;

/// <summary>
/// Réglages Identity de l'application, partagés avec les tests pour qu'ils éprouvent la même
/// politique que la prod.
/// </summary>
public static class IdentityPolicy
{
    /// <summary>Tentatives ratées tolérées avant verrouillage du compte.</summary>
    public const int MaxFailedAccessAttempts = 5;

    /// <summary>Durée du verrouillage : assez longue pour ruiner un essai en rafale, assez courte
    /// pour qu'un compte verrouillé par erreur se débloque seul, sans intervention.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Longueur minimale d'un mot de passe pour tout compte (FR-034). On vise une phrase de passe
    /// (« Jambon-Saloir-2026-Mamie »), longue plutôt que tordue : plus facile à taper sur un téléphone
    /// pour un utilisateur non technique, et bien plus coûteuse à deviner.
    /// </summary>
    public const int MinPasswordLength = 20;

    public const int MinUniqueChars = 10;

    /// <summary>
    /// Longueur minimale pour un administrateur, qui détient le plus de pouvoir (ADR-011) :
    /// « Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions ». Appliquée par
    /// <see cref="AdminPasswordValidator"/>, qui connaît le rôle du compte.
    /// </summary>
    public const int AdminMinPasswordLength = 32;

    public const int AdminMinUniqueChars = 12;

    public static void Configure(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;

        // La longueur fait l'essentiel ; les règles de composition restent, une phrase de passe
        // séparée par des tirets et portant un chiffre les satisfait sans effort.
        options.Password.RequiredLength = MinPasswordLength;
        options.Password.RequiredUniqueChars = MinUniqueChars;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
    }

    /// <summary>
    /// Ajoute ce que les options ne savent pas exprimer : la règle propre au rôle et les messages en
    /// français. À appeler partout où Identity est configuré, application comme tests.
    /// </summary>
    public static IdentityBuilder AddSaloirPasswordRules(this IdentityBuilder builder) =>
        builder
            .AddPasswordValidator<AdminPasswordValidator>()
            .AddErrorDescriber<FrenchIdentityErrorDescriber>();
}
