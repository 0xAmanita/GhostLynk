namespace backend_deob.Services;

public interface IPasswordHashService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
