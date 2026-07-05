namespace Application.Dtos.Trips;

/// <summary>
/// Represents a single day in a trip itinerary, with its scheduled destinations.
/// </summary>
public class ItineraryDayDto
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>1-based index of this day within the trip (Day 1, Day 2, …).</summary>
    public int DayIndex { get; set; }

    /// <summary>
    /// Destinations scheduled on this day, ordered by <see cref="TripDestinationDto.Position"/>.
    /// Empty when no destinations have been added to this day yet.
    /// </summary>
    public IReadOnlyList<TripDestinationDto> TripDestinations { get; set; } = [];
}
