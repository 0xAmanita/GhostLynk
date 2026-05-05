using System.ComponentModel.DataAnnotations;

namespace backend_deob.DTOs;

public class ResetPasswordRequest
{
    [Required]
    public required string Token { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }

    [Required]
    [Compare("Password")]
    public required string PasswordConfirm { get; set; }
}
