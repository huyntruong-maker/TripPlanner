namespace Application.Dtos.Trips;

public class TripDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Populated only in detail responses.</summary>
    public IReadOnlyList<ItineraryDayDto> ItineraryDays { get; set; } = [];

    /// <summary>Populated only in detail responses.</summary>
    public IReadOnlyList<TripDestinationDto> SavedPlaces { get; set; } = [];
}
