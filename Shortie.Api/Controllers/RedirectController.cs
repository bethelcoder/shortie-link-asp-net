using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shortie.Api.Data;
using Shortie.Api.Entities;
using UAParser;

namespace Shortie.Api.Controllers;

[ApiController]
[Route("r")]
public class RedirectController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public RedirectController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code, CancellationToken cancellationToken)
    {
        var shortUrl = await _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => x.ShortCode == code || x.CustomAlias == code, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound(new { message = "Short URL not found." });
        }

        if (shortUrl.ExpiresAtUtc.HasValue && shortUrl.ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = "This short URL has expired." });
        }

        var parsedClient = Parser.GetDefault().Parse(Request.Headers.UserAgent.ToString());

        var rawReferrer = Request.Query["src"].FirstOrDefault() 
            ?? Request.Query["ref"].FirstOrDefault()
            ?? (Request.Headers.Referer.ToString() is { Length: > 0 } referer ? referer : "Direct");

        string referrer;
        if (string.Equals(rawReferrer, "share", StringComparison.OrdinalIgnoreCase))
        {
            referrer = "Share";
        }
        else if (string.Equals(rawReferrer, "qr", StringComparison.OrdinalIgnoreCase) || string.Equals(rawReferrer, "qrcode", StringComparison.OrdinalIgnoreCase))
        {
            referrer = "QR Code";
        }
        else
        {
            referrer = rawReferrer;
        }

        var clickEvent = new ClickEvent
        {
            ShortUrlId = shortUrl.Id,
            Browser = parsedClient.UA.Family ?? "Unknown",
            Device = parsedClient.Device.Family ?? "Unknown",
            Country = Request.Headers["CF-IPCountry"].FirstOrDefault()
                ?? Request.Headers["X-Country-Code"].FirstOrDefault()
                ?? "Unknown",
            Referrer = referrer,
            ClickedAtUtc = DateTime.UtcNow
        };

        shortUrl.ClickCount += 1;
        shortUrl.LastAccessedAtUtc = DateTime.UtcNow;
        shortUrl.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.ClickEvents.Add(clickEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Redirect(shortUrl.OriginalUrl);
    }
}
