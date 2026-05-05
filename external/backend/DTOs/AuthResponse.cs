namespace backend_deob.DTOs;

public class AuthResponse
{
    public required string Token { get; set; }
    public required UserProfile User { get; set; }
}

public class UserProfile
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
