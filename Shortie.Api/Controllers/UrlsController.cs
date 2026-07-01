using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Shortie.Api.Data;
using Shortie.Api.DTOs.Urls;
using Shortie.Api.Entities;
using Shortie.Api.Security;
using Shortie.Api.Services;

namespace Shortie.Api.Controllers;

[ApiController]
[Authorize(Policy = "UserOrAdmin")]
[Route("api/[controller]")]
public class UrlsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IShortCodeGenerator _shortCodeGenerator;
    private readonly IConfiguration _configuration;

    public UrlsController(ApplicationDbContext dbContext, IShortCodeGenerator shortCodeGenerator, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _shortCodeGenerator = shortCodeGenerator;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<ActionResult<ShortUrlResponseDto>> Create(CreateShortUrlRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            var aliasUsed = await _dbContext.ShortUrls.AnyAsync(
                x => x.CustomAlias == request.CustomAlias || x.ShortCode == request.CustomAlias,
                cancellationToken);

            if (aliasUsed)
            {
                return Conflict(new { message = "Custom alias is already taken." });
            }
        }

        var shortCode = string.IsNullOrWhiteSpace(request.CustomAlias)
            ? await GenerateUniqueCodeAsync(cancellationToken)
            : _shortCodeGenerator.Generate();

        var shortUrl = new ShortUrl
        {
            OriginalUrl = request.OriginalUrl,
            ShortCode = shortCode,
            CustomAlias = string.IsNullOrWhiteSpace(request.CustomAlias) ? null : request.CustomAlias,
            ExpiresAtUtc = request.ExpiresAtUtc,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ShortUrls.Add(shortUrl);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = shortUrl.Id }, ToResponse(shortUrl));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ShortUrlResponseDto>>> List(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var urls = await _dbContext.ShortUrls
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(urls.Select(ToResponse));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/all")]
    public async Task<ActionResult<IReadOnlyCollection<ShortUrlResponseDto>>> AdminListAll(CancellationToken cancellationToken)
    {
        var urls = await _dbContext.ShortUrls
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(urls.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShortUrlResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shortUrl = await _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(shortUrl));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShortUrlResponseDto>> Update(Guid id, UpdateShortUrlRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shortUrl = await _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.CustomAlias) && request.CustomAlias != shortUrl.CustomAlias)
        {
            var aliasUsed = await _dbContext.ShortUrls.AnyAsync(
                x => x.Id != id && (x.CustomAlias == request.CustomAlias || x.ShortCode == request.CustomAlias),
                cancellationToken);

            if (aliasUsed)
            {
                return Conflict(new { message = "Custom alias is already taken." });
            }
        }

        shortUrl.OriginalUrl = request.OriginalUrl;
        shortUrl.CustomAlias = string.IsNullOrWhiteSpace(request.CustomAlias) ? null : request.CustomAlias;
        shortUrl.ExpiresAtUtc = request.ExpiresAtUtc;
        shortUrl.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(shortUrl));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shortUrl = await _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound();
        }

        _dbContext.ShortUrls.Remove(shortUrl);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<ActionResult<UrlAnalyticsResponseDto>> GetAnalytics(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shortUrl = await _dbContext.ShortUrls
            .Include(x => x.ClickEvents)
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound();
        }

        var browser = shortUrl.ClickEvents
            .GroupBy(x => x.Browser)
            .ToDictionary(x => x.Key, x => x.Count());

        var device = shortUrl.ClickEvents
            .GroupBy(x => x.Device)
            .ToDictionary(x => x.Key, x => x.Count());

        var country = shortUrl.ClickEvents
            .GroupBy(x => x.Country)
            .ToDictionary(x => x.Key, x => x.Count());

        var referrer = shortUrl.ClickEvents
            .GroupBy(x => x.Referrer)
            .ToDictionary(x => x.Key, x => x.Count());

        var response = new UrlAnalyticsResponseDto(
            shortUrl.Id,
            shortUrl.ClickCount,
            shortUrl.LastAccessedAtUtc,
            browser,
            device,
            country,
            referrer);

        return Ok(response);
    }

    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shortUrl = await _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, cancellationToken);

        if (shortUrl is null)
        {
            return NotFound();
        }

        var payload = ToPublicShortLink(shortUrl) + "?src=qr";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(20);

        return File(bytes, "image/png", $"{shortUrl.Id}.png");
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = _shortCodeGenerator.Generate();
            var exists = await _dbContext.ShortUrls.AnyAsync(x => x.ShortCode == code || x.CustomAlias == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Failed to generate a unique short code. Please try again.");
    }

    private string ToPublicShortLink(ShortUrl shortUrl)
    {
        var baseUrl = _configuration["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
        var code = shortUrl.CustomAlias ?? shortUrl.ShortCode;
        return $"{baseUrl}/r/{code}";
    }

    private ShortUrlResponseDto ToResponse(ShortUrl shortUrl)
    {
        return new ShortUrlResponseDto(
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.ShortCode,
            shortUrl.CustomAlias,
            ToPublicShortLink(shortUrl),
            shortUrl.ExpiresAtUtc,
            shortUrl.ClickCount,
            shortUrl.LastAccessedAtUtc,
            shortUrl.CreatedAtUtc,
            shortUrl.UpdatedAtUtc);
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
