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

    public static void Configure(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
    }
}
