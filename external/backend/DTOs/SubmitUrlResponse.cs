namespace backend_deob.DTOs;

public class SubmitUrlResponse
{
    public required string ObfuscatedUrl { get; set; }
    public required string Nickname { get; set; }
    public DateTime CreatedAt { get; set; }
}
