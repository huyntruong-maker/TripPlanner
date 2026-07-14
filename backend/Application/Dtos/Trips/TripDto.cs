namespace Application.Dtos.Trips;

/// <summary>A trip with its itinerary structure; itinerary days are empty in list responses.</summary>
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

    /// <summary>Itinerary days ordered by day index; populated only in detail responses.</summary>
    public IReadOnlyList<ItineraryDayDto> ItineraryDays { get; set; } = [];
}
