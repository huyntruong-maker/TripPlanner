namespace Application.Dtos.Trips;

/// <summary>
/// Represents a trip with its itinerary structure.
/// In list responses <see cref="ItineraryDays"/> is always an empty collection.
/// In the detail response <see cref="ItineraryDays"/> is populated with days and their destinations.
/// </summary>
public class TripDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Null when the user has not yet set dates for this trip.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Null when the user has not yet set dates for this trip.</summary>
    public DateOnly? EndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Itinerary days ordered by <see cref="ItineraryDayDto.DayIndex"/>.
    /// Populated only in detail responses; empty in list responses.
    /// </summary>
    public IReadOnlyList<ItineraryDayDto> ItineraryDays { get; set; } = [];
}
