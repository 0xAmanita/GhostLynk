namespace backend_deob.Models;

public class IpMetadata
{
    public Guid Id { get; set; }
    public Guid UrlEntryId { get; set; }
    public string? IpAddress { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? Org { get; set; }
    public string? Timezone { get; set; }
    public DateTime FetchedAt { get; set; }

    public UrlEntry UrlEntry { get; set; } = null!;
}
