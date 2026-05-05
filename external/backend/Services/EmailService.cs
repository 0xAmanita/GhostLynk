using System.Text;
using System.Text.Json;

namespace backend_deob.Services;

public class EmailService : IEmailService
{
    private readonly string _resendApiKey;
    private readonly string _frontendUrl;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration configuration, HttpClient httpClient)
    {
        _resendApiKey = configuration["RESEND_API_KEY"] ?? throw new InvalidOperationException("RESEND_API_KEY not configured");
        _frontendUrl = configuration["FRONTEND_URL"] ?? "http://localhost:5173";
        _httpClient = httpClient;
    }

    public async Task<string?> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var resetLink = $"{_frontendUrl}/reset-password?token={resetToken}";
        var emailBody = new
        {
            from = "GhostLynk <noreply@ghostlynk.com>",
            to = new[] { toEmail },
            subject = "Reset Your Password - GhostLynk",
            html = $@"
                <h2>Password Reset Request</h2>
                <p>You requested to reset your password. Click the link below to proceed:</p>
                <p><a href=""{resetLink}"">Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
            "
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Add("Authorization", $"Bearer {_resendApiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(emailBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResendResponse>(responseContent);
        return result?.id;
    }

    private class ResendResponse
    {
        public string? id { get; set; }
    }
}
