using Microsoft.EntityFrameworkCore;
using backend_deob.Models;

namespace backend_deob.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UrlEntry> UrlEntries { get; set; }
    public DbSet<IpMetadata> IpMetadata { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UrlEntry configuration
        modelBuilder.Entity<UrlEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ObfuscatedUrl).IsUnique();
            entity.HasIndex(e => e.IsLocked);
            entity.Property(e => e.OriginalUrl).IsRequired();
            entity.Property(e => e.ObfuscatedUrl).IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasskeyHash).IsRequired();
            entity.Property(e => e.FailedAttempts).HasDefaultValue(0);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UrlEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // IpMetadata configuration
        modelBuilder.Entity<IpMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UrlEntryId).IsUnique();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(10);
            entity.Property(e => e.Org).HasMaxLength(255);
            entity.Property(e => e.Timezone).HasMaxLength(100);
            entity.Property(e => e.FetchedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.UrlEntry)
                .WithOne(u => u.IpMetadata)
                .HasForeignKey<IpMetadata>(e => e.UrlEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.SessionToken).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PasswordResetToken configuration
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.TokenHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ResendEmailId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
