namespace Application.Dtos.Destinations;

/// <summary>Full destination detail; optional fields are null/empty rather than rejected when unavailable.</summary>
public class DestinationDetailDto
{
    /// <summary>Provider-specific identifier (OpenTripMap xid or Foursquare fsq_id).</summary>
    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    /// <summary>Primary category, e.g. "cultural", "natural", "food".</summary>
    public string? Category { get; set; }

    /// <summary>Additional tags / kinds from the provider.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Short description or editorial summary when available.</summary>
    public string? Description { get; set; }

    /// <summary>Ordered photo URLs; empty list means show a placeholder image (F2-US2).</summary>
    public IReadOnlyList<string> Photos { get; set; } = [];

    /// <summary>Street / city / country address string when available.</summary>
    public string? Address { get; set; }

    /// <summary>Official website URL when available.</summary>
    public string? Website { get; set; }

    /// <summary>Null means show "Opening hours not available" in the UI (F2-US4).</summary>
    public OpeningHoursDto? OpeningHours { get; set; }

    /// <summary>Popularity or rating score, 0–10 scale where available.</summary>
    public double? Rating { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
