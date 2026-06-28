namespace Application.Dtos.Destinations;

/// <summary>
/// Represents structured opening-hours information for a destination.
/// All fields are optional; the entire object is null when the provider
/// does not supply hours data.
/// </summary>
public class OpeningHoursDto
{
    /// <summary>
    /// Human-readable summary of opening hours, e.g. "Mon-Fri 09:00-17:00".
    /// Populated from the provider's display string when available.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Ordered list of per-day strings, e.g. ["Monday: 9:00 AM – 5:00 PM", …].
    /// Empty when the provider does not return structured day-level data.
    /// </summary>
    public IReadOnlyList<string> WeekdayText { get; set; } = [];

    /// <summary>True when the provider explicitly states the place is open now; null when unknown.</summary>
    public bool? IsOpenNow { get; set; }
}
