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
    /// pour qu'un compte partagé verrouillé par erreur se débloque seul, sans intervention.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Longueur minimale d'un mot de passe. Le compte est exposé sur Internet : on vise une phrase de
    /// passe (« Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions »), longue plutôt que tordue — plus
    /// facile à recopier pour un utilisateur non technique, et bien plus coûteuse à deviner.
    /// </summary>
    public const int MinPasswordLength = 32;

    public static void Configure(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;

        // La longueur fait l'essentiel ; les règles de composition restent, une phrase de passe
        // séparée par des tirets et portant un chiffre les satisfait sans effort.
        options.Password.RequiredLength = MinPasswordLength;
        options.Password.RequiredUniqueChars = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
    }
}
