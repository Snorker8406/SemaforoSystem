using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Products;

/// <summary>
/// Query parameters specific to the Products endpoint.
/// </summary>
public class ProductQueryParameters : QueryParameters
{
    /// <summary>Filter by brand ID.</summary>
    public int? BrandId { get; set; }

    /// <summary>Filter by category ID.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Filter products that have schools associated.</summary>
    public bool? HasSchools { get; set; }

    /// <summary>Filter by model (contains).</summary>
    public string? Model { get; set; }

    /// <summary>Filter by serialize flag.</summary>
    public bool? Serialize { get; set; }

    /// <summary>Filter products that have stock.</summary>
    public bool? HasStock { get; set; }
}
