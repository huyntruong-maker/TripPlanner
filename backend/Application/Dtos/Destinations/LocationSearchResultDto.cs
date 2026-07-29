namespace Application.Dtos.Destinations;

public class LocationSearchResultDto
{
    public IReadOnlyList<LocationDto> Items { get; set; } = [];

    public int TotalCount { get; set; }
}
