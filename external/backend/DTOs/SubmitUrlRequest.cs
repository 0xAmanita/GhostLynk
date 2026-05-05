using System.ComponentModel.DataAnnotations;

namespace backend_deob.DTOs;

public class SubmitUrlRequest
{
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public required string Url { get; set; }

    [Required(ErrorMessage = "Nickname is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Nickname must be between 1 and 50 characters")]
    public required string Nickname { get; set; }

    [Required(ErrorMessage = "Passkey is required")]
    public required string Passkey { get; set; }
}
