namespace Application.Dtos.Trips;

public class ItineraryDayDto
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>1-based (Day 1, Day 2, …).</summary>
    public int DayIndex { get; set; }

    public IReadOnlyList<TripDestinationDto> TripDestinations { get; set; } = [];
}
