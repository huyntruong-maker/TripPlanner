namespace Application.Dtos.Destinations;

public class AttractionSearchResultDto
{
    public IReadOnlyList<AttractionDto> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public bool IsEmpty => Items.Count == 0;
}
