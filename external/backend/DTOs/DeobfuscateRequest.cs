using System.ComponentModel.DataAnnotations;

namespace backend_deob.DTOs;

public class DeobfuscateRequest
{
    [Required]
    public string ObfuscatedText { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Nickname { get; set; } = string.Empty;

    [Required]
    public string Passkey { get; set; } = string.Empty;
}
