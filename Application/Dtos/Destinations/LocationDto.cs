namespace Application.Dtos.Destinations;

/// <summary>
/// Represents a single geocoding result — a city or country with coordinates.
/// </summary>
public class LocationDto
{
    public required string Name { get; set; }

    /// <summary>Human-readable label combining the city/country hierarchy.</summary>
    public required string DisplayName { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    /// <summary>The type of location: city, region, country, etc.</summary>
    public string? LocationType { get; set; }

    /// <summary>Country the location belongs to.</summary>
    public string? Country { get; set; }
}
