using backend_deob.Data;
using backend_deob.DTOs;
using backend_deob.Models;
using backend_deob.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_deob.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtService _jwtService;

    public AuthController(
        ApplicationDbContext context,
        IPasswordHashService passwordHashService,
        IJwtService jwtService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _jwtService = jwtService;
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
}
