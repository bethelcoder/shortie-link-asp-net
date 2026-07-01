namespace Shortie.Api.Services;

public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
