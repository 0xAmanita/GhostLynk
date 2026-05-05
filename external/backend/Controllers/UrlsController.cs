using Microsoft.AspNetCore.Mvc;
using backend_deob.Data;
using backend_deob.DTOs;
using backend_deob.Services;
using backend_deob.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_deob.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRateLimitService _rateLimitService;
    private readonly IObfuscationService _obfuscationService;
    private readonly IIpInfoService _ipInfoService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ILogger<UrlsController> _logger;

    public UrlsController(
        ApplicationDbContext context,
        IRateLimitService rateLimitService,
        IObfuscationService obfuscationService,
        IIpInfoService ipInfoService,
        IPasswordHashService passwordHashService,
        ILogger<UrlsController> logger)
    {
        _context = context;
        _rateLimitService = rateLimitService;
        _obfuscationService = obfuscationService;
        _ipInfoService = ipInfoService;
        _passwordHashService = passwordHashService;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        return HttpContext.Items["UserId"] as Guid?;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitUrlRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        // Check rate limit
        var canSubmit = await _rateLimitService.CanSubmitAsync(userId.Value);
        if (!canSubmit)
        {
            return StatusCode(429, new { error = "Rate limit exceeded. Please wait 5 minutes between submissions." });
        }

        // Validate URL format
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return BadRequest(new { error = "Invalid URL format" });
        }

        try
        {
            // Hash passkey
            var passkeyHash = _passwordHashService.HashPassword(request.Passkey);

            // Obfuscate URL
            var obfuscatedUrl = _obfuscationService.Obfuscate(request.Url);

            // Get client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            // Fetch IP metadata
            var ipMetadata = await _ipInfoService.FetchIpMetadataAsync(ipAddress);

            // Create URL entry
            var urlEntry = new UrlEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                OriginalUrl = request.Url,
                ObfuscatedUrl = obfuscatedUrl,
                Nickname = request.Nickname,
                PasskeyHash = passkeyHash,
                FailedAttempts = 0,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UrlEntries.Add(urlEntry);

            // Add IP metadata if available
            if (ipMetadata != null)
            {
                ipMetadata.Id = Guid.NewGuid();
                ipMetadata.UrlEntryId = urlEntry.Id;
                _context.IpMetadata.Add(ipMetadata);
            }

            // Update rate limit timestamp
            await _rateLimitService.UpdateSubmitTimestampAsync(userId.Value);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Submit), new SubmitUrlResponse
            {
                ObfuscatedUrl = obfuscatedUrl,
                Nickname = request.Nickname,
                CreatedAt = urlEntry.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting URL for user {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while submitting the URL" });
        }
    }
}
