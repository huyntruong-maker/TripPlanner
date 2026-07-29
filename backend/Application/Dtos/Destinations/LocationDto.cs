namespace Application.Dtos.Destinations;

public class LocationDto
{
    public required string Name { get; set; }

    public required string DisplayName { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? LocationType { get; set; }

    public string? Country { get; set; }
}
