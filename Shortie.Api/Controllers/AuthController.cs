using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortie.Api.DTOs.Auth;
using Shortie.Api.Entities;
using Shortie.Api.Security;
using Shortie.Api.Services;

namespace Shortie.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        var tokenPair = await _jwtTokenService.CreateTokenPairAsync(user, GetIpAddress(), cancellationToken);
        return Ok(new AuthResponseDto(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var tokenPair = await _jwtTokenService.CreateTokenPairAsync(user, GetIpAddress(), cancellationToken);
        return Ok(new AuthResponseDto(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var tokenPair = await _jwtTokenService.RefreshAsync(request.RefreshToken, GetIpAddress(), cancellationToken);
        if (tokenPair is null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        return Ok(new AuthResponseDto(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        await _jwtTokenService.RevokeRefreshTokenAsync(
            request.RefreshToken,
            "User requested logout",
            GetIpAddress(),
            cancellationToken);

        return NoContent();
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
