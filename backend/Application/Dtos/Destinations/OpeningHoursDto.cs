namespace Application.Dtos.Destinations;

public class OpeningHoursDto
{
    public string? DisplayText { get; set; }

    public IReadOnlyList<string> WeekdayText { get; set; } = [];

    public bool? IsOpenNow { get; set; }
}
