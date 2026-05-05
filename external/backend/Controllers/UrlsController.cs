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
}
