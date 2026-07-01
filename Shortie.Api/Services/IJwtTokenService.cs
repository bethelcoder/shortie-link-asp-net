using Shortie.Api.Entities;

namespace Shortie.Api.Services;

public interface IJwtTokenService
{
    Task<TokenPair> CreateTokenPairAsync(
        ApplicationUser user,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<TokenPair?> RefreshAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string refreshToken,
        string? reason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
