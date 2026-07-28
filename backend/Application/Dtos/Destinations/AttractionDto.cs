namespace Application.Dtos.Destinations;

public class AttractionDto
{
    /// <summary>OpenTripMap xid or Foursquare fsq_id.</summary>
    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    public string? Category { get; set; }

    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>0-10 scale.</summary>
    public double? Rating { get; set; }

    public string? ThumbnailUrl { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? Address { get; set; }

    // Detail-only fields below: null/empty when returned from list queries.

    public string? Description { get; set; }

    public IReadOnlyList<string> Photos { get; set; } = [];

    public string? Website { get; set; }

    public OpeningHoursDto? OpeningHours { get; set; }
}
