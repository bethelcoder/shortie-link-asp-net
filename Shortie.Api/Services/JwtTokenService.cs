using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shortie.Api.Data;
using Shortie.Api.Entities;

namespace Shortie.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<TokenPair> CreateTokenPairAsync(
        ApplicationUser user,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenMinutes"));
        var accessToken = await CreateAccessTokenAsync(user, expiresAtUtc);
        var refreshTokenValue = CreateSecureRefreshToken();
        var refreshTokenHash = ComputeSha256(refreshTokenValue);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenDays")),
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TokenPair(accessToken, refreshTokenValue, expiresAtUtc);
    }

    public async Task<TokenPair?> RefreshAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(refreshToken);
        var token = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.User is null)
        {
            return null;
        }

        if (token.IsRevoked)
        {
            await RevokeAllActiveRefreshTokensForUserAsync(
                token.UserId,
                "Refresh token reuse detected. Session family revoked.",
                ipAddress,
                cancellationToken);

            return null;
        }

        if (token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        var rotatedTokenValue = CreateSecureRefreshToken();
        var rotatedTokenHash = ComputeSha256(rotatedTokenValue);

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedReason = "Rotated";
        token.ReplacedByTokenHash = rotatedTokenHash;
        token.ReplacedByIp = ipAddress;

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = rotatedTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenDays")),
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenMinutes"));
        var accessToken = await CreateAccessTokenAsync(token.User, expiresAtUtc);

        return new TokenPair(accessToken, rotatedTokenValue, expiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string? reason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(refreshToken);
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.IsRevoked)
        {
            return;
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedReason = reason ?? "Revoked";
        token.ReplacedByIp = ipAddress;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user, DateTime expiresAtUtc)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        };
        claims.AddRange(roleClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateSecureRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private async Task RevokeAllActiveRefreshTokensForUserAsync(
        string userId,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAtUtc = DateTime.UtcNow;
            activeToken.RevokedReason = reason;
            activeToken.ReplacedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
