namespace Application.Dtos.Destinations;

/// <summary>
/// Full detail record for a single destination / attraction, returned by
/// <c>GET /api/v1/destinations/{providerPlaceId}</c>.
/// All optional fields are null or empty when the provider does not supply them;
/// the response is always returned rather than rejected (graceful partial data).
/// </summary>
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

    /// <summary>
    /// Ordered list of photo URLs. Empty when no photos are available.
    /// Callers show a placeholder image when this list is empty (F2-US2).
    /// </summary>
    public IReadOnlyList<string> Photos { get; set; } = [];

    /// <summary>Street / city / country address string when available.</summary>
    public string? Address { get; set; }

    /// <summary>Official website URL when available.</summary>
    public string? Website { get; set; }

    /// <summary>
    /// Structured opening-hours data. Null when the provider does not supply it.
    /// The UI must show "Opening hours not available" when this is null (F2-US4).
    /// </summary>
    public OpeningHoursDto? OpeningHours { get; set; }

    /// <summary>Popularity or rating score, 0–10 scale where available.</summary>
    public double? Rating { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
