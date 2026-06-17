namespace Application.Dtos.Base;

public class SearchDto
{
    /// <summary>
    ///     Start page.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    ///     Number of items per page.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    ///     Sort column.
    /// </summary>
    public string? Column { get; set; }

    /// <summary>
    ///     Order by asc or desc.
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    ///     Keyword.
    /// </summary>
    public string? Keyword { get; set; }

    public bool Ascending => Direction == "asc";
}