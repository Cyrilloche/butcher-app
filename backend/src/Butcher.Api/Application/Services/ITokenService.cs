using Butcher.Api.Domain.Entities;

namespace Butcher.Api.Application.Services;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AppUser user);

    string GenerateRefreshTokenValue();

    string HashRefreshToken(string rawValue);

    TimeSpan RefreshTokenLifetime { get; }
}
