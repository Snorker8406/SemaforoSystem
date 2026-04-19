using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.ProductCombos;

// ═══════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════

/// <summary>Response for a product combo (header).</summary>
public class ProductComboResponse
{
    public long ProductComboId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int VisualDefinitionCount { get; set; }
    public int SchoolCount { get; set; }
    public List<VisualDefinitionResponse> VisualDefinitions { get; set; } = [];
}

/// <summary>Lightweight combo summary for lookups / dropdowns.</summary>
public class ProductComboSummary
{
    public long ProductComboId { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>Response for a visual definition inside a combo.</summary>
public class VisualDefinitionResponse
{
    public long ProductComboVisualDefinitionId { get; set; }
    public long ProductComboId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal? FixedPriceAmount { get; set; }
    public string DiscountType { get; set; } = null!;
    public decimal? DiscountValue { get; set; }
    public int? PriceListId { get; set; }
    public string? PriceListName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool HasImage { get; set; }
    public long? ImageId { get; set; }
    public int ComponentCount { get; set; }
    public List<ComponentResponse> Components { get; set; } = [];
    public List<ComboSchoolInfo> Schools { get; set; } = [];
}

/// <summary>Response for a component inside a visual definition.</summary>
public class ComponentResponse
{
    public long ProductComboComponentId { get; set; }
    public long ProductComboVisualDefinitionId { get; set; }
    public string ComponentType { get; set; } = null!;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public string? VisualDefinitionProductName { get; set; }
    public List<ComponentVariantInfo> VisualDefinitionVariants { get; set; } = [];
    public int? EmbroideryId { get; set; }
    public string? EmbroideryName { get; set; }
    public int Quantity { get; set; }
    public string? Placement { get; set; }
    public bool IsRequired { get; set; }
    public decimal? ExtraPrice { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Variant info for a PRODUCT_VISUAL_DEFINITION component.</summary>
public class ComponentVariantInfo
{
    public string SystemName { get; set; } = null!;
    public string VariantValue { get; set; } = null!;
}

/// <summary>Lightweight school info embedded in combo response.</summary>
public class ComboSchoolInfo
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════════
//  Query parameters
// ═══════════════════════════════════════════════════════════════════

/// <summary>Query parameters for the combos list endpoint.</summary>
public class ProductComboQueryParameters : Common.QueryParameters
{
    public bool? IsActive { get; set; }
    public int? SchoolId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Combo (header)
// ═══════════════════════════════════════════════════════════════════

public class CreateProductComboRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

public class UpdateProductComboRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Visual Definition
// ═══════════════════════════════════════════════════════════════════

public class CreateVisualDefinitionRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public decimal? FixedPriceAmount { get; set; }

    [StringLength(20)]
    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public int? PriceListId { get; set; }

    public bool? IsActive { get; set; }

    public int? SortOrder { get; set; }

    public List<int>? SchoolIds { get; set; }
}

public class UpdateVisualDefinitionRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public decimal? FixedPriceAmount { get; set; }

    [StringLength(20)]
    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public int? PriceListId { get; set; }

    public bool? IsActive { get; set; }

    public int? SortOrder { get; set; }

    public List<int>? SchoolIds { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Component
// ═══════════════════════════════════════════════════════════════════

public class CreateComponentRequest
{
    [Required, StringLength(40)]
    public string ComponentType { get; set; } = null!;

    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? EmbroideryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [StringLength(50)]
    public string? Placement { get; set; }

    public bool? IsRequired { get; set; }
    public decimal? ExtraPrice { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateComponentRequest
{
    [Required, StringLength(40)]
    public string ComponentType { get; set; } = null!;

    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? EmbroideryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [StringLength(50)]
    public string? Placement { get; set; }

    public bool? IsRequired { get; set; }
    public decimal? ExtraPrice { get; set; }
    public int? SortOrder { get; set; }
}
