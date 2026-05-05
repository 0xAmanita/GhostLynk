using backend_deob.Models;

namespace backend_deob.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    Guid? ValidateToken(string token);
}
