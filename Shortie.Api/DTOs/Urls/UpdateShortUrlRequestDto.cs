namespace Shortie.Api.DTOs.Urls;

public record UpdateShortUrlRequestDto(string OriginalUrl, string? CustomAlias, DateTime? ExpiresAtUtc);
