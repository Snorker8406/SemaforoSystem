using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Schools;

/// <summary>
/// Query parameters specific to the Schools endpoint.
/// </summary>
public class SchoolQueryParameters : QueryParameters
{
    /// <summary>Filter by school level ID.</summary>
    public int? SchoolLevelId { get; set; }

    /// <summary>Filter by city.</summary>
    public string? Ciudad { get; set; }

    /// <summary>Filter by state.</summary>
    public string? State { get; set; }
}
