namespace Butcher.Api.Application.Services;

public record AccessTokenResult(string Value, DateTimeOffset ExpiresAtUtc);
