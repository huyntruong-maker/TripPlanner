namespace Application.Dtos.Destinations;

/// <summary>
/// Paginated result wrapper for an attractions search.
/// </summary>
public class AttractionSearchResultDto
{
    public IReadOnlyList<AttractionDto> Items { get; set; } = [];

    public int TotalCount { get; set; }

    /// <summary>True when the provider returned zero results for the given coordinates.</summary>
    public bool IsEmpty => Items.Count == 0;
}
