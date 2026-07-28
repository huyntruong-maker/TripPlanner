namespace Application.Dtos.Trips;

public class TripDestinationDto
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    /// <summary>Null when unscheduled (in "Saved Places").</summary>
    public Guid? ItineraryDayId { get; set; }

    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    public string? Category { get; set; }

    public string? ThumbnailUrl { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public int Position { get; set; }
}
