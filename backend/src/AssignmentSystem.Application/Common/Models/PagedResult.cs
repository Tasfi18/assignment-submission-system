namespace AssignmentSystem.Application.Common.Models;

/// <summary>Standard envelope returned by every list endpoint.</summary>
public class PagedResult<T>
{
    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
