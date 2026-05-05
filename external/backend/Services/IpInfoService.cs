using backend_deob.Models;
using System.Text.Json;

namespace backend_deob.Services;

public class IpInfoService : IIpInfoService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiToken;
    private readonly ILogger<IpInfoService> _logger;

    public IpInfoService(HttpClient httpClient, IConfiguration configuration, ILogger<IpInfoService> logger)
    {
        _httpClient = httpClient;
        _apiToken = configuration["IPINFO_TOKEN"];
        _logger = logger;
    }

    public async Task<IpMetadata?> FetchIpMetadataAsync(string ipAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiToken))
            {
                _logger.LogWarning("IpInfo API token not configured");
                return null;
            }

            var url = $"https://ipinfo.io/{ipAddress}?token={_apiToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("IpInfo API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var ipInfoResponse = JsonSerializer.Deserialize<IpInfoResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ipInfoResponse == null)
            {
                _logger.LogError("Failed to deserialize IpInfo API response");
                return null;
            }

            return new IpMetadata
            {
                IpAddress = ipInfoResponse.Ip,
                City = ipInfoResponse.City,
                Region = ipInfoResponse.Region,
                Country = ipInfoResponse.Country,
                Org = ipInfoResponse.Org,
                Timezone = ipInfoResponse.Timezone,
                FetchedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching IP metadata for {IpAddress}", ipAddress);
            return null;
        }
    }

    private class IpInfoResponse
    {
        public string? Ip { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? Org { get; set; }
        public string? Timezone { get; set; }
    }
}
