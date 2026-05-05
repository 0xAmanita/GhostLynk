using System.ComponentModel.DataAnnotations;

namespace backend_deob.DTOs;

public class LoginRequest
{
    [Required]
    public required string EmailOrUsername { get; set; }

    [Required]
    public required string Password { get; set; }
}
