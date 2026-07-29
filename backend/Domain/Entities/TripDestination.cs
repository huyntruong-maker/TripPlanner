using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.IEntities;

namespace Domain.Entities;

[Table("TripDestinations")]
public class TripDestination : BaseEntity, IIsDeletedEntity
{
    public Guid TripId { get; set; }

    /// <summary>Null means the destination is unscheduled ("Saved Places").</summary>
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

    public int Position { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Trip Trip { get; set; } = null!;

    public virtual ItineraryDay? ItineraryDay { get; set; }
}
