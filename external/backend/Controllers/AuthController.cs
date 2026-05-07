using backend_deob.Data;
using backend_deob.DTOs;
using backend_deob.Models;
using backend_deob.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace backend_deob.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public AuthController(
        ApplicationDbContext context,
        IPasswordHashService passwordHashService,
        IJwtService jwtService,
        IEmailService emailService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Email already exists" });

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Username already exists" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Address = request.Address,
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Success = true, Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

        if (user == null || !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new ApiResponse<AuthResponse> { Success = false, Message = "Invalid credentials" });

        var token = _jwtService.GenerateToken(user);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var authResponse = new AuthResponse
        {
            Token = token,
            User = new UserProfile
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };

        return Ok(new ApiResponse<AuthResponse> { Success = true, Data = authResponse });
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Ok(new ApiResponse<object> { Success = true, Message = "If the email exists, a reset link has been sent" });

        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);

        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))
        );

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var emailId = await _emailService.SendPasswordResetEmailAsync(user.Email, token);
        if (emailId != null)
        {
            resetToken.ResendEmailId = emailId;
            await _context.SaveChangesAsync();
        }

        return Ok(new ApiResponse<object> { Success = true, Message = "If the email exists, a reset link has been sent" });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.Token))
        );

        var resetToken = await _context.PasswordResetTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.UsedAt == null);

        if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid or expired token" });

        resetToken.User.PasswordHash = _passwordHashService.HashPassword(request.Password);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Success = true, Message = "Password reset successful" });
    }
}