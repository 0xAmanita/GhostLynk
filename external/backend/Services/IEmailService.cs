namespace backend_deob.Services;

public interface IEmailService
{
    Task<string?> SendPasswordResetEmailAsync(string toEmail, string resetToken);
}
