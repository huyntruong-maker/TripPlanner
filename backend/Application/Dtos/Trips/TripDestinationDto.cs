namespace Application.Dtos.Trips;

/// <summary>A destination saved to a trip, optionally scheduled to an itinerary day.</summary>
public class TripDestinationDto
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    /// <summary>Null when the destination is unscheduled (in "Saved Places").</summary>
    public Guid? ItineraryDayId { get; set; }

    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    public string? Category { get; set; }

    public string? ThumbnailUrl { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    /// <summary>Ordering position within the itinerary day (or saved-places list).</summary>
    public int Position { get; set; }
}
