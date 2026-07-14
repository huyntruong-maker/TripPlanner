namespace Application.Dtos.Destinations;

/// <summary>Structured opening-hours info; the whole object is null when the provider lacks hours data.</summary>
public class OpeningHoursDto
{
    /// <summary>Human-readable summary, e.g. "Mon-Fri 09:00-17:00".</summary>
    public string? DisplayText { get; set; }

    /// <summary>Per-day strings, e.g. "Monday: 9:00 AM – 5:00 PM"; empty if no structured data.</summary>
    public IReadOnlyList<string> WeekdayText { get; set; } = [];

    /// <summary>True when the provider explicitly states the place is open now; null when unknown.</summary>
    public bool? IsOpenNow { get; set; }
}
