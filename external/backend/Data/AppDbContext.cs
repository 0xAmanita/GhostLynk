using Microsoft.EntityFrameworkCore;
using backend_deob.Models;

namespace backend_deob.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UrlEntry> UrlEntries { get; set; }
    public DbSet<IpMetadata> IpMetadata { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // UrlEntry entity configuration
        modelBuilder.Entity<UrlEntry>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ObfuscatedUrl).IsUnique();
            entity.HasIndex(e => e.IsLocked);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UrlEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // IpMetadata entity configuration
        modelBuilder.Entity<IpMetadata>(entity =>
        {
            entity.HasIndex(e => e.UrlEntryId).IsUnique();

            entity.HasOne(e => e.UrlEntry)
                .WithOne(u => u.IpMetadata)
                .HasForeignKey<IpMetadata>(e => e.UrlEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Session entity configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PasswordResetToken entity configuration
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
