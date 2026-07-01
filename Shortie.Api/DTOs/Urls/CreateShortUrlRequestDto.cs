namespace Shortie.Api.DTOs.Urls;

public record CreateShortUrlRequestDto(string OriginalUrl, string? CustomAlias, DateTime? ExpiresAtUtc);
