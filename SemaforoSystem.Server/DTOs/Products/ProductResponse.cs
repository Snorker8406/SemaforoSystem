namespace SemaforoSystem.Server.DTOs.Products;

/// <summary>
/// Response DTO representing a product entity.
/// </summary>
public class ProductResponse
{
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? Model { get; set; }
    public string? Comments { get; set; }
    public long? SerialCount { get; set; }
    public bool? Serialize { get; set; }
    public DateTime? CreateDate { get; set; }

    // Brand
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }

    // Size system
    public int? SizeSystemId { get; set; }
    public string? SizeSystemName { get; set; }

    // Categories
    public List<CategoryInfo> Categories { get; set; } = [];

    // Related counts & summary data
    public int SchoolCount { get; set; }
    public bool HasPicture { get; set; }
    public int StockTotal { get; set; }
    public decimal? LatestCost { get; set; }

    // Variant systems
    public List<VariantSystemInfo> VariantSystems { get; set; } = [];
}

public class CategoryInfo
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VariantSystemInfo
{
    public int ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// School info with its school level, returned by the /schools sub-resource.
/// Now linked through product_visual_definition_schools.
/// </summary>
public class ProductSchoolInfo
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SchoolLevelId { get; set; }
    public string SchoolLevelName { get; set; } = string.Empty;
    public long ProductVisualDefinitionId { get; set; }
}

/// <summary>
/// Lightweight summary of an InventoryItemDefinition belonging to a product,
/// used in the expandable row of the products table.
/// </summary>
public class ItemDefinitionSummary
{
    public int InventoryItemDefinitionId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string? NameSnapshot { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsActive { get; set; }
    public int? SizeId { get; set; }
    public string? SizeValue { get; set; }
    public bool HasImage { get; set; }
    public List<ItemDefinitionVariantInfo> Variants { get; set; } = [];
}

public class ItemDefinitionVariantInfo
{
    public int ProductVariantId { get; set; }
    public string VariantValue { get; set; } = string.Empty;
    public string? SystemName { get; set; }
}

// ── Visual Definitions (grouped by variant combination) ──

/// <summary>
/// A visual definition groups item definitions that share the same variant combination
/// but differ only by size. Each card in the UI represents one visual definition.
/// </summary>
public class VisualDefinitionGroup
{
    public long ProductVisualDefinitionId { get; set; }
    public string VariantsHash { get; set; } = string.Empty;
    public List<ItemDefinitionVariantInfo> Variants { get; set; } = [];
    public bool HasImage { get; set; }
    /// <summary>Item definitions (sizes) under this visual definition.</summary>
    public List<VisualDefinitionItem> Items { get; set; } = [];
    /// <summary>Schools linked to this visual definition.</summary>
    public List<VisualDefinitionSchoolInfo> Schools { get; set; } = [];
}

/// <summary>
/// Lightweight school info for a visual definition card.
/// </summary>
public class VisualDefinitionSchoolInfo
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SchoolLevelName { get; set; } = string.Empty;
}

/// <summary>
/// An item definition within a visual definition group (size-level detail).
/// Includes the effective price resolved via ITEM_DEF → VISUAL_DEF → PRODUCT fallback.
/// </summary>
public class VisualDefinitionItem
{
    public int InventoryItemDefinitionId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string? NameSnapshot { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsActive { get; set; }
    public int? SizeId { get; set; }
    public string? SizeValue { get; set; }
    public int? SizeOrder { get; set; }

    // ── Effective price (resolved per item definition) ──
    /// <summary>The effective price amount, or null if no price is set.</summary>
    public decimal? PriceAmount { get; set; }
    /// <summary>BASE or PROMO.</summary>
    public string? PriceKind { get; set; }
    /// <summary>Which scope resolved: ITEM_DEFINITION, VISUAL_DEFINITION, or PRODUCT.</summary>
    public string? PriceScope { get; set; }
    /// <summary>When a PROMO is active, the BASE price for strikethrough display.</summary>
    public decimal? BasePriceAmount { get; set; }
    /// <summary>Promo name if applicable.</summary>
    public string? PromoName { get; set; }
}
