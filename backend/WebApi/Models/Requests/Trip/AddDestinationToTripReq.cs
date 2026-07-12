namespace WebApi.Models.Requests.Trip;

public class AddDestinationToTripReq
{
    public Guid? ItineraryDayId { get; set; }

    public string? ProviderPlaceId { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public string? ThumbnailUrl { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }
}
