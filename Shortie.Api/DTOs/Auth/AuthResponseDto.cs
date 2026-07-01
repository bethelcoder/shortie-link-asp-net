namespace Shortie.Api.DTOs.Auth;

public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
