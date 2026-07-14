namespace Application.Dtos.Base;

public class SearchDto
{
    public int Start { get; set; }

    public int Length { get; set; }

    public string? Column { get; set; }

    /// <summary>Expected values: "asc" or "desc".</summary>
    public string? Direction { get; set; }

    public string? Keyword { get; set; }

    public bool Ascending => Direction == "asc";
}