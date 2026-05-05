namespace backend_deob.Models;

public class UrlEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string OriginalUrl { get; set; }
    public required string ObfuscatedUrl { get; set; }
    public required string Nickname { get; set; }
    public required string PasskeyHash { get; set; }
    public int FailedAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public IpMetadata? IpMetadata { get; set; }
}
