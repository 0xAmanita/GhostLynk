using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_deob.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend_deob.Services;

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly int _expirationHours;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured");
        _expirationHours = int.Parse(configuration["JWT_EXPIRATION_HOURS"] ?? "24");
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("username", user.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(_expirationHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public Guid? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "sub").Value;
            return Guid.Parse(userId);
        }
        catch
        {
            return null;
        }
    }
}
