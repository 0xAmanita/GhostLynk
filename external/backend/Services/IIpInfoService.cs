using backend_deob.Models;

namespace backend_deob.Services;

public interface IIpInfoService
{
    Task<IpMetadata?> FetchIpMetadataAsync(string ipAddress);
}
