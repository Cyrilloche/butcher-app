using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

/// <summary>
/// Gestion des comptes par l'administrateur (ADR-011). Un compte n'est jamais supprimé, et l'outil
/// garde toujours au moins un administrateur actif.
/// </summary>
public interface IAccountService
{
    /// <summary>Tous les comptes, actifs et désactivés.</summary>
    Task<List<AccountDto>> GetAllAsync();

    Task<AccountDto> CreateAsync(CreateAccountRequest request);

    /// <summary>
    /// Modifie le nom et le rôle. Une promotion exige un nouveau mot de passe conforme au rôle
    /// administrateur ; une rétrogradation est refusée si elle retire le dernier administrateur actif.
    /// </summary>
    Task<AccountDto> UpdateAsync(Guid accountId, UpdateAccountRequest request);

    /// <summary>
    /// Désactive un compte et ferme ses sessions. Refusé sur son propre compte et sur le dernier
    /// administrateur actif.
    /// </summary>
    Task DeactivateAsync(Guid accountId, Guid actingAccountId);

    Task ReactivateAsync(Guid accountId);

    /// <summary>Remplace le mot de passe, lève un éventuel verrouillage et ferme les sessions.</summary>
    Task ResetPasswordAsync(Guid accountId, string newPassword);
}
