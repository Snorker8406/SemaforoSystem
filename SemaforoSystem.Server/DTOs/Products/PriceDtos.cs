using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Products;

/// <summary>
/// Response DTO for a product price entry.
/// </summary>
public class ProductPriceResponse
{
    public int PriceId { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public DateTime CreateDate { get; set; }

    // Size (optional)
    public int? SizeId { get; set; }
    public string? SizeValue { get; set; }

    // Variant (optional)
    public int? VariantId { get; set; }
    public string? VariantValue { get; set; }

    // Combo (optional)
    public int? ProductComboId { get; set; }
}

/// <summary>
/// Request body for creating a product price.
/// </summary>
public class CreateProductPriceRequest
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    public decimal Price { get; set; }

    public int? SizeId { get; set; }

    public int? VariantId { get; set; }

    public int? ProductComboId { get; set; }
}

/// <summary>
/// Request body for updating a product price (only price value).
/// </summary>
public class UpdateProductPriceRequest
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    public decimal Price { get; set; }
}
