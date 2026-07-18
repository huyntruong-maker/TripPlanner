namespace WebApi.Models.Requests.Trip;

public class MoveTripDestinationReq
{
    /// <summary>Null moves the destination to "Saved Places".</summary>
    public Guid? ItineraryDayId { get; set; }

    /// <summary>1-based target position within the bucket; null/out-of-range appends at the end.</summary>
    public int? Position { get; set; }
}
