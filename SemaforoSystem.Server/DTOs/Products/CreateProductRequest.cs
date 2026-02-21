using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Products;

/// <summary>
/// Request body for creating a new product.
/// </summary>
public class CreateProductRequest
{
    [StringLength(500, MinimumLength = 1)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Barcode { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    public string? Comments { get; set; }

    public long? SerialCount { get; set; }

    public bool? Serialize { get; set; }

    public int? BrandId { get; set; }

    /// <summary>List of category IDs to associate with the product.</summary>
    public List<int> CategoryIds { get; set; } = [];
}
