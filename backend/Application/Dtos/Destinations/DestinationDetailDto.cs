namespace Application.Dtos.Destinations;

public class DestinationDetailDto
{
    /// <summary>OpenTripMap xid or Foursquare fsq_id.</summary>
    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    public string? Category { get; set; }

    public IReadOnlyList<string> Tags { get; set; } = [];

    public string? Description { get; set; }

    /// <summary>Empty list means show a placeholder image (F2-US2).</summary>
    public IReadOnlyList<string> Photos { get; set; } = [];

    public string? Address { get; set; }

    public string? Website { get; set; }

    /// <summary>Null means show "Opening hours not available" in the UI (F2-US4).</summary>
    public OpeningHoursDto? OpeningHours { get; set; }

    /// <summary>0-10 scale.</summary>
    public double? Rating { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
