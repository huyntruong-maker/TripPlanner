using Domain.Constants;

namespace Application.Dtos.Base;

public class Pagination<T> where T : class
{
    public Pagination(IQueryable<T> iQuery, int pageNumber = GlobalConstants.PageConfig.Start,
        int pageSize = GlobalConstants.PageConfig.Length)
    {
        Items = [];
        TotalItems = 0;
        TotalPages = 0;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Query = iQuery;
        ExecutePaginate();
    }

    public Pagination(IEnumerable<T> iQuery, int pageNumber = GlobalConstants.PageConfig.Start,
        int pageSize = GlobalConstants.PageConfig.Length)
    {
        Items = [];
        TotalItems = 0;
        TotalPages = 0;
        PageNumber = pageNumber;
        PageSize = pageSize;
        QueryEnumerable = iQuery;
        ExecutePaginate2();
    }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }

    public List<T> Items { get; set; }

    private IQueryable<T>? Query { get; }

    private IEnumerable<T>? QueryEnumerable { get; }

    private void ExecutePaginate()
    {
        if (Query == null) return;

        if (PageNumber <= 0) PageNumber = GlobalConstants.PageConfig.Start;

        if (PageSize <= 0) PageSize = GlobalConstants.PageConfig.Length;

        if (PageSize > GlobalConstants.PageConfig.MaxLength) PageSize = GlobalConstants.PageConfig.MaxLength;

        TotalItems = Query.Count();
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        Items = [.. Query.Skip((PageNumber - GlobalConstants.PageConfig.Start) * PageSize).Take(PageSize)];
    }

    private void ExecutePaginate2()
    {
        if (QueryEnumerable == null) return;

        if (PageNumber <= 0) PageNumber = GlobalConstants.PageConfig.Start;

        if (PageSize <= 0) PageSize = GlobalConstants.PageConfig.Length;

        if (PageSize > GlobalConstants.PageConfig.MaxLength) PageSize = GlobalConstants.PageConfig.MaxLength;

        TotalItems = QueryEnumerable.Count();
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        Items = [.. QueryEnumerable.Skip((PageNumber - GlobalConstants.PageConfig.Start) * PageSize).Take(PageSize)];
    }
}