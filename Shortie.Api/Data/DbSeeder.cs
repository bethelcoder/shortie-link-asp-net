using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shortie.Api.Entities;
using Shortie.Api.Security;

namespace Shortie.Api.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DbSeeder(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        var admin = await SeedUserAsync("Admin User", "admin@shortie.dev", "Admin123!", AppRoles.Admin);
        var demo = await SeedUserAsync("Demo User", "demo@shortie.dev", "Demo123!", AppRoles.User);

        if (!await _dbContext.ShortUrls.AnyAsync(cancellationToken))
        {
            _dbContext.ShortUrls.AddRange(
                new ShortUrl
                {
                    OriginalUrl = "https://learn.microsoft.com/aspnet/core",
                    ShortCode = "dotnet",
                    CustomAlias = "docs",
                    ClickCount = 4,
                    LastAccessedAtUtc = DateTime.UtcNow.AddDays(-1),
                    CreatedByUserId = admin.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new ShortUrl
                {
                    OriginalUrl = "https://react.dev/learn",
                    ShortCode = "react1",
                    CustomAlias = "react-guide",
                    ClickCount = 2,
                    LastAccessedAtUtc = DateTime.UtcNow.AddDays(-2),
                    CreatedByUserId = demo.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-6),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
                });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { AppRoles.Admin, AppRoles.User };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task<ApplicationUser> SeedUserAsync(string fullName, string email, string password, string role)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed seeding user '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
