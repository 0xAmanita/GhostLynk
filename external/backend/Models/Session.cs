namespace backend_deob.Models;

public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string SessionToken { get; set; }
    public DateTime? LastSubmitAt { get; set; }
    public DateTime? LastDeobfuscateAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}
