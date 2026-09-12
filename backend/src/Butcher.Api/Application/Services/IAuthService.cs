using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);

    Task<AuthResult> RefreshAsync(string refreshTokenValue);

    Task LogoutAsync(string refreshTokenValue);

    Task<MeDto> GetAccountAsync(Guid accountId);

    /// <summary>
    /// Remplace le mot de passe d'un compte, sous réserve du mot de passe actuel et de la politique de
    /// son rôle. Révoque les sessions des autres appareils ; celle dont le refresh token est fourni est
    /// conservée.
    /// </summary>
    Task ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword, string? currentRefreshTokenValue);
}
