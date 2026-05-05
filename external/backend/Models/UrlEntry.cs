using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_deob.Models;

[Table("url_entries")]
public class UrlEntry
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("original_url")]
    public string OriginalUrl { get; set; } = string.Empty;

    [Required]
    [Column("obfuscated_url")]
    public string ObfuscatedUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [Required]
    [Column("passkey_hash")]
    public string PasskeyHash { get; set; } = string.Empty;

    [Required]
    [Column("failed_attempts")]
    public int FailedAttempts { get; set; } = 0;

    [Required]
    [Column("is_locked")]
    public bool IsLocked { get; set; } = false;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    public IpMetadata? IpMetadata { get; set; }
}
