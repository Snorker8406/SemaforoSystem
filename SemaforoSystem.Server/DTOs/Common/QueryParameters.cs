using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Common;

/// <summary>
/// Base query parameters for paginated and sortable endpoints.
/// </summary>
public class QueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(value, MaxPageSize);
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public string? Search { get; set; }
}
