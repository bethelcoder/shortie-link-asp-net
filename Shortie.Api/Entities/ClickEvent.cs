namespace Shortie.Api.Entities;

public class ClickEvent
{
    public Guid Id { get; set; }
    public Guid ShortUrlId { get; set; }
    public ShortUrl? ShortUrl { get; set; }
    public string Browser { get; set; } = "Unknown";
    public string Device { get; set; } = "Unknown";
    public string Country { get; set; } = "Unknown";
    public string Referrer { get; set; } = "Direct";
    public DateTime ClickedAtUtc { get; set; } = DateTime.UtcNow;
}
