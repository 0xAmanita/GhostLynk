using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_deob.Models;

[Table("sessions")]
public class Session
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("session_token")]
    public string SessionToken { get; set; } = string.Empty;

    [Column("last_submit_at")]
    public DateTime? LastSubmitAt { get; set; }

    [Column("last_deobfuscate_at")]
    public DateTime? LastDeobfuscateAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
