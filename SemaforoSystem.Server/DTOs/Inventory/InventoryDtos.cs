using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Inventory;

// ═══════════════════════════════════════════════════════════════════
//  Request DTOs
// ═══════════════════════════════════════════════════════════════════

/// <summary>Request to create an ENTRY transaction (goods receipt).</summary>
public class CreateEntryRequest
{
    [Required]
    public int SiteId { get; set; }

    public string? Reference { get; set; }

    public string? Comments { get; set; }

    [Required, MinLength(1)]
    public List<CreateEntryLineRequest> Lines { get; set; } = [];
}

/// <summary>A single line within an ENTRY request.</summary>
public class CreateEntryLineRequest
{
    /// <summary>
    /// If known, pass the existing definition id directly.
    /// When null, the system resolves/creates it from Product/Size/Variants.
    /// </summary>
    public int? InventoryItemDefinitionId { get; set; }

    /// <summary>Required when InventoryItemDefinitionId is null.</summary>
    public int? ProductId { get; set; }

    /// <summary>Optional size (nullable).</summary>
    public int? SizeId { get; set; }

    /// <summary>Optional variant ids to resolve the definition.</summary>
    public List<int>? VariantIds { get; set; }

    /// <summary>Whether the item is serialized. Required when creating a new definition.</summary>
    public bool? IsSerialized { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal? UnitCost { get; set; }

}

// ═══════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════

/// <summary>Full response for an inventory transaction (header + lines).</summary>
public class InventoryTransactionResponse
{
    public long InventoryTransactionId { get; set; }
    public string TransactionType { get; set; } = null!;
    public DateTime TransactionDate { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
    public string? UserId { get; set; }
    public List<InventoryTransactionLineResponse> Lines { get; set; } = [];
}

/// <summary>Response for a single ledger line.</summary>
public class InventoryTransactionLineResponse
{
    public long InventoryTransactionLineId { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int InventoryItemDefinitionId { get; set; }
    public string? SkuCode { get; set; }
    public string? NameSnapshot { get; set; }
    public int? QtyDelta { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? UnitPrice { get; set; }
    public List<SerialItemInfo>? SerialItems { get; set; }
}

/// <summary>Lightweight serial item info returned within a line.</summary>
public class SerialItemInfo
{
    public long InventorySerialItemId { get; set; }
    public string Barcode { get; set; } = null!;
    public long? SerialNumber { get; set; }
    public short Status { get; set; }
}

/// <summary>Lightweight site info for selectors.</summary>
public class SiteLookup
{
    public int SiteId { get; set; }
    public string Name { get; set; } = null!;
}
