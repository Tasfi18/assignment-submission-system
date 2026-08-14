using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Application.Common.Models;

/// <summary>Query string parameters shared by the list endpoints.</summary>
public class PageRequest
{
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>Free text search. Meaning depends on the endpoint.</summary>
    public string? Search { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
