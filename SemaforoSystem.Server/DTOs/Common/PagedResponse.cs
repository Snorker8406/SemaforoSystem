namespace SemaforoSystem.Server.DTOs.Common;

/// <summary>
/// Generic wrapper for paginated API responses.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
