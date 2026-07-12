using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("DestinationCache")]
public class DestinationCache
{
    [Key]
    [MaxLength(256)]
    public required string ProviderPlaceId { get; set; }

    /// <summary>
    /// Raw JSON payload from the upstream provider (OpenTripMap / Foursquare).
    /// Stored as jsonb on PostgreSQL.
    /// </summary>
    public required string PayloadJson { get; set; }

    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
