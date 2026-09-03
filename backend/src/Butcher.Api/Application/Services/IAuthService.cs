namespace Butcher.Api.Application.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);

    Task<AuthResult> RefreshAsync(string refreshTokenValue);

    Task LogoutAsync(string refreshTokenValue);
}
