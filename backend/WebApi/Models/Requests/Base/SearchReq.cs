using System.ComponentModel.DataAnnotations;
using Domain.Constants;

namespace WebApi.Models.Requests.Base;

public class SearchReq : IPaginator, ISorting
{
    private string _keyword = string.Empty;

    public SearchReq()
    {
        Start = GlobalConstants.PageConfig.Start;
        Length = GlobalConstants.PageConfig.Length;
        Column = string.Empty;
        Direction = string.Empty;
        Keyword = string.Empty;
    }

    [MaxLength(100, ErrorMessage = "The maximum length of the Keyword is 100 characters")]
    public string Keyword
    {
        get => _keyword;
        set => _keyword = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    /// <summary>Default: 1.</summary>
    public int Start { get; set; }

    /// <summary>Default: 10.</summary>
    public int Length { get; set; }

    public string Column { get; set; }

    /// <summary>Expected values: "asc" or "desc".</summary>
    public string Direction { get; set; }
}