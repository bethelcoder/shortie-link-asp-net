using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shortie.Api.Entities;

namespace Shortie.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ShortUrl>()
            .HasIndex(x => x.ShortCode)
            .IsUnique();

        builder.Entity<ShortUrl>()
            .HasIndex(x => x.CustomAlias)
            .IsUnique()
            .HasFilter("[CustomAlias] IS NOT NULL");

        builder.Entity<ShortUrl>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClickEvent>()
            .HasOne(x => x.ShortUrl)
            .WithMany(x => x.ClickEvents)
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Entity<RefreshToken>()
            .Property(x => x.TokenHash)
            .HasMaxLength(128);

        builder.Entity<RefreshToken>()
            .Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.Entity<RefreshToken>()
            .Property(x => x.RevokedReason)
            .HasMaxLength(200);

        builder.Entity<RefreshToken>()
            .Property(x => x.CreatedByIp)
            .HasMaxLength(100);

        builder.Entity<RefreshToken>()
            .Property(x => x.ReplacedByIp)
            .HasMaxLength(100);

        builder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
