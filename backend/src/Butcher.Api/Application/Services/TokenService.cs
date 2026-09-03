using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Butcher.Api.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Butcher.Api.Application.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(configuration.GetValue("Jwt:RefreshTokenLifetimeDays", 30));

    public AccessTokenResult CreateAccessToken(AppUser user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 15));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string HashRefreshToken(string rawValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawValue)));
}
