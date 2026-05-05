namespace backend_deob.DTOs;

public class DeobfuscateResponse
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
