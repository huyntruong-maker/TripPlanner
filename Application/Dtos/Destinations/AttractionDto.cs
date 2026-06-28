namespace Application.Dtos.Destinations;

/// <summary>
/// Represents a single point-of-interest / attraction enriched from OpenTripMap and Foursquare.
/// </summary>
public class AttractionDto
{
    /// <summary>Provider-specific identifier (OpenTripMap xid or Foursquare fsq_id).</summary>
    public required string ProviderPlaceId { get; set; }

    public required string Name { get; set; }

    /// <summary>Primary category, e.g. "cultural", "natural", "food".</summary>
    public string? Category { get; set; }

    /// <summary>Additional tags / kinds from the provider.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Popularity or rating score, 0–10 scale where available.</summary>
    public double? Rating { get; set; }

    /// <summary>URL of the thumbnail image, null when unavailable.</summary>
    public string? ThumbnailUrl { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    /// <summary>Street address when available.</summary>
    public string? Address { get; set; }

    // -------------------------------------------------------------------------
    // Detail-only fields — populated by GetAttractionDetailAsync; null/empty
    // when returned from list queries (GetAttractionsAsync).
    // -------------------------------------------------------------------------

    /// <summary>Short description or editorial summary when available.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ordered list of photo URLs. Empty for list results; populated for detail calls.
    /// </summary>
    public IReadOnlyList<string> Photos { get; set; } = [];

    /// <summary>Official website URL when available.</summary>
    public string? Website { get; set; }

    /// <summary>Structured opening-hours data. Null when unavailable.</summary>
    public OpeningHoursDto? OpeningHours { get; set; }
}
