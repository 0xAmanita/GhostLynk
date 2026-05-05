using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_deob.Models;

[Table("ip_metadata")]
public class IpMetadata
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("url_entry_id")]
    public Guid UrlEntryId { get; set; }

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Column("region")]
    public string? Region { get; set; }

    [MaxLength(10)]
    [Column("country")]
    public string? Country { get; set; }

    [MaxLength(255)]
    [Column("org")]
    public string? Org { get; set; }

    [MaxLength(100)]
    [Column("timezone")]
    public string? Timezone { get; set; }

    [Required]
    [Column("fetched_at")]
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UrlEntryId")]
    public UrlEntry UrlEntry { get; set; } = null!;
}
