namespace Shortie.Api.Entities;

public class ShortUrl
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string? CustomAlias { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ClickEvent> ClickEvents { get; set; } = new List<ClickEvent>();
}
