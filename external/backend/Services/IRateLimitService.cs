namespace backend_deob.Services;

public interface IRateLimitService
{
    Task<bool> CanSubmitAsync(Guid userId);
    Task UpdateSubmitTimestampAsync(Guid userId);
    Task<bool> CanDeobfuscateAsync(Guid userId);
    Task UpdateDeobfuscateTimestampAsync(Guid userId);
}
