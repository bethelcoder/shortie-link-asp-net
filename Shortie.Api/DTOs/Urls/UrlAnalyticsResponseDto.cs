namespace Shortie.Api.DTOs.Urls;

public record UrlAnalyticsResponseDto(
    Guid UrlId,
    int ClickCount,
    DateTime? LastAccessedAtUtc,
    IDictionary<string, int> BrowserBreakdown,
    IDictionary<string, int> DeviceBreakdown,
    IDictionary<string, int> CountryBreakdown,
    IDictionary<string, int> ReferrerBreakdown
);
