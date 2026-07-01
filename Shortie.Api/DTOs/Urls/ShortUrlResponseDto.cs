namespace Shortie.Api.DTOs.Urls;

public record ShortUrlResponseDto(
    Guid Id,
    string OriginalUrl,
    string ShortCode,
    string? CustomAlias,
    string ShortLink,
    DateTime? ExpiresAtUtc,
    int ClickCount,
    DateTime? LastAccessedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
