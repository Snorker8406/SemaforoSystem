using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Pricing;

// ═══════════════════════════════════════════════════════════════════════════
//  Price List DTOs
// ═══════════════════════════════════════════════════════════════════════════

public class PriceListResponse
{
    public int PriceListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ProductEntryCount { get; set; }
    public int VisualDefinitionEntryCount { get; set; }
    public int ItemDefinitionEntryCount { get; set; }
}

public class CreatePriceListRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(3)]
    public string Currency { get; set; } = "MXN";

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdatePriceListRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(3)]
    public string Currency { get; set; } = "MXN";

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}

// ═══════════════════════════════════════════════════════════════════════════
//  Price Entry DTOs (shared shape for all 3 scopes)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unified response for a price entry, regardless of scope.
/// The "Scope" field indicates PRODUCT, VISUAL_DEFINITION, or ITEM_DEFINITION.
/// </summary>
public class PriceEntryResponse
{
    public long PriceEntryId { get; set; }
    public int PriceListId { get; set; }
    public string? PriceListName { get; set; }

    /// <summary>PRODUCT | VISUAL_DEFINITION | ITEM_DEFINITION</summary>
    public string Scope { get; set; } = string.Empty;

    // Target identifiers (only the relevant one is non-null)
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }

    // Resolved label for UI display
    public string? TargetLabel { get; set; }

    public decimal PriceAmount { get; set; }
    public string PriceKind { get; set; } = "BASE";
    public int Priority { get; set; }
    public string? PromoName { get; set; }
    public string? PromoCode { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? Reason { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Set Base Price request
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Sets a new BASE price. Closes the previous active BASE for the same target & list.
/// Exactly one of ProductId / ProductVisualDefinitionId / InventoryItemDefinitionId must be set.
/// </summary>
public class SetBasePriceRequest
{
    [Required]
    public int PriceListId { get; set; }

    // Target — exactly one must be set
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }

    [Required, Range(0, (double)decimal.MaxValue)]
    public decimal PriceAmount { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Add Promo request
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Adds a PROMO price entry with explicit validity window.
/// Exactly one of ProductId / ProductVisualDefinitionId / InventoryItemDefinitionId must be set.
/// </summary>
public class AddPromoPriceRequest
{
    [Required]
    public int PriceListId { get; set; }

    // Target — exactly one must be set
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }

    [Required, Range(0, (double)decimal.MaxValue)]
    public decimal PriceAmount { get; set; }

    public int Priority { get; set; }

    [StringLength(120)]
    public string? PromoName { get; set; }

    [StringLength(60)]
    public string? PromoCode { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    [Required]
    public DateTime ValidTo { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Effective Price response
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// The resolved effective price for a specific item definition,
/// including which scope it came from and whether a promo is active.
/// </summary>
public class EffectivePriceResponse
{
    public decimal PriceAmount { get; set; }
    public string PriceKind { get; set; } = "BASE";
    public string Scope { get; set; } = string.Empty;
    public long PriceEntryId { get; set; }
    public string? PromoName { get; set; }
    public string? PromoCode { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// When a PROMO is returned, this contains the BASE price for strikethrough display.
    /// </summary>
    public decimal? BasePriceAmount { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Price History query parameters
// ═══════════════════════════════════════════════════════════════════════════

public class PriceHistoryQuery
{
    /// <summary>PRODUCT | VISUAL_DEFINITION | ITEM_DEFINITION</summary>
    [Required]
    public string Scope { get; set; } = string.Empty;

    /// <summary>The target id depending on scope.</summary>
    [Required]
    public long TargetId { get; set; }

    public int? PriceListId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
