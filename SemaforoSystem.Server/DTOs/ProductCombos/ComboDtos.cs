using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.ProductCombos;

// ═══════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════

/// <summary>Response for a product combo (header) with its details.</summary>
public class ProductComboResponse
{
    public int ProductComboId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreateDate { get; set; }
    public int DetailCount { get; set; }
    public int PriceCount { get; set; }
    public List<ProductComboDetailResponse> Details { get; set; } = [];
}

/// <summary>Lightweight combo summary for lookups / dropdowns.</summary>
public class ProductComboSummary
{
    public int ProductComboId { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>Response for a single combo detail line.</summary>
public class ProductComboDetailResponse
{
    public int ProductComboDetailId { get; set; }
    public int ProductComboId { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? EmbroideryId { get; set; }
    public string? EmbroideryName { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Query parameters
// ═══════════════════════════════════════════════════════════════════

/// <summary>Query parameters for the combos list endpoint.</summary>
public class ProductComboQueryParameters : Common.QueryParameters
{
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Combo (header)
// ═══════════════════════════════════════════════════════════════════

public class CreateProductComboRequest
{
    [Required, StringLength(250)]
    public string Name { get; set; } = null!;

    [StringLength(250)]
    public string? Description { get; set; }
}

public class UpdateProductComboRequest
{
    [Required, StringLength(250)]
    public string Name { get; set; } = null!;

    [StringLength(250)]
    public string? Description { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Combo Detail (lines)
// ═══════════════════════════════════════════════════════════════════

public class CreateProductComboDetailRequest
{
    public int? ProductId { get; set; }
    public int? EmbroideryId { get; set; }
}

public class UpdateProductComboDetailRequest
{
    public int? ProductId { get; set; }
    public int? EmbroideryId { get; set; }
}
