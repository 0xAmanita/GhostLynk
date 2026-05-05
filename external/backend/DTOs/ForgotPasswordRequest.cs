using System.ComponentModel.DataAnnotations;

namespace backend_deob.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
