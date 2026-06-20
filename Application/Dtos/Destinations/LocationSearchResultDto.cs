namespace Application.Dtos.Destinations;

/// <summary>
/// Paginated result wrapper for a location (city/country) search.
/// </summary>
public class LocationSearchResultDto
{
    public IReadOnlyList<LocationDto> Items { get; set; } = [];

    public int TotalCount { get; set; }
}
