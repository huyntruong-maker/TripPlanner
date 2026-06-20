using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("TripDestinations")]
public class TripDestination
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    /// <summary>
    /// Null means the destination is in "Saved Places" — not yet scheduled to a specific day.
    /// </summary>
    public Guid? ItineraryDayId { get; set; }

    [MaxLength(256)]
    public required string ProviderPlaceId { get; set; }

    [MaxLength(300)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    /// <summary>
    /// Ordering position within the itinerary day (or saved places list).
    /// </summary>
    public int Position { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual Trip Trip { get; set; } = null!;

    public virtual ItineraryDay? ItineraryDay { get; set; }
}
