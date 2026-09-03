namespace Butcher.Api.Application.Services;

// Transport interne service -> contrôleur uniquement : contient la valeur brute du refresh token,
// qui ne doit jamais apparaître dans un corps de réponse JSON (seulement dans le cookie httpOnly).
// Volontairement en dehors de Application/Dtos pour ne pas être confondu avec un contrat d'API.
public record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
