using backend_deob.Data;
using Microsoft.EntityFrameworkCore;

namespace backend_deob.Services;

public class RateLimitService : IRateLimitService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(5);

    public RateLimitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanSubmitAsync(Guid userId)
    {
        var session = await _context.Sessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session == null || session.LastSubmitAt == null)
            return true;

        return DateTime.UtcNow - session.LastSubmitAt.Value >= _rateLimitWindow;
    }

    public async Task UpdateSubmitTimestampAsync(Guid userId)
    {
        var session = await _context.Sessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.LastSubmitAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CanDeobfuscateAsync(Guid userId)
    {
        var session = await _context.Sessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session == null || session.LastDeobfuscateAt == null)
            return true;

        return DateTime.UtcNow - session.LastDeobfuscateAt.Value >= _rateLimitWindow;
    }

    public async Task UpdateDeobfuscateTimestampAsync(Guid userId)
    {
        var session = await _context.Sessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.LastDeobfuscateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
