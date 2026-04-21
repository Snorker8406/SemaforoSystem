using System.ComponentModel.DataAnnotations;
using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Inventory;

// ═══════════════════════════════════════════════════════════════════
//  Query Parameters
// ═══════════════════════════════════════════════════════════════════

public class BalanceQueryParams : QueryParameters
{
    public int? SiteId { get; set; }
    public int? ProductId { get; set; }
    public bool? IsSerialized { get; set; }
    public bool? IsActive { get; set; }
}

public class LedgerQueryParams : QueryParameters
{
    public int? SiteId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public string? TransactionType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class SerialItemQueryParams : QueryParameters
{
    public int? SiteId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public short? Status { get; set; }
}

public class TransactionQueryParams : QueryParameters
{
    public string? TransactionType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

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
    public int? InventoryItemDefinitionId { get; set; }
    public int? ProductId { get; set; }
    public int? SizeId { get; set; }
    public List<int>? VariantIds { get; set; }
    public bool? IsSerialized { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal? UnitCost { get; set; }
}

/// <summary>Request to create a generic ledger transaction.</summary>
public class CreateTransactionRequest
{
    [Required, StringLength(30)]
    public string TransactionType { get; set; } = null!;

    [Required]
    public int SiteId { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public string? Comments { get; set; }

    [Required, MinLength(1)]
    public List<CreateTransactionLineRequest> Lines { get; set; } = [];
}

public class CreateTransactionLineRequest
{
    [Required]
    public int InventoryItemDefinitionId { get; set; }

    [Required]
    public int QtyDelta { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal? UnitCost { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal? UnitPrice { get; set; }

    /// <summary>For serialized items: barcode list of specific units to move.</summary>
    public List<string>? Barcodes { get; set; }

    public int? SourceSiteId { get; set; }
    public int? TargetSiteId { get; set; }
}

/// <summary>Request to create or commit a reservation.</summary>
public class CreateReservationRequest
{
    [Required]
    public int SiteId { get; set; }

    [Required]
    public int InventoryItemDefinitionId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public long? SaleOrderId { get; set; }

    /// <summary>Expiration time in minutes from now. Null = no expiration.</summary>
    public int? ExpiresInMinutes { get; set; }

    /// <summary>For serialized items: specific serial item ids to reserve.</summary>
    public List<long>? SerialItemIds { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════

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
    public int? SourceSiteId { get; set; }
    public int? TargetSiteId { get; set; }
    public List<SerialItemInfo>? SerialItems { get; set; }
}

public class SerialItemInfo
{
    public long InventorySerialItemId { get; set; }
    public string Barcode { get; set; } = null!;
    public long? SerialNumber { get; set; }
    public short Status { get; set; }
}

public class BalanceResponse
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = null!;
    public int InventoryItemDefinitionId { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? NameSnapshot { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? SizeId { get; set; }
    public string? SizeName { get; set; }
    public bool IsSerialized { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Available => OnHand - Reserved;
    public DateTime UpdatedAt { get; set; }
}

public class ItemDefinitionResponse
{
    public int InventoryItemDefinitionId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? SizeId { get; set; }
    public string? SizeName { get; set; }
    public bool IsSerialized { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? NameSnapshot { get; set; }
    public bool IsActive { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VariantInfo> Variants { get; set; } = [];
}

public class VariantInfo
{
    public int ProductVariantId { get; set; }
    public string? VariantValue { get; set; }
    public string? SystemName { get; set; }
}

public class SerialItemDetailResponse
{
    public long InventorySerialItemId { get; set; }
    public int InventoryItemDefinitionId { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? NameSnapshot { get; set; }
    public int CurrentSiteId { get; set; }
    public string? CurrentSiteName { get; set; }
    public string Barcode { get; set; } = null!;
    public long? SerialNumber { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public List<SerialMoveInfo> Moves { get; set; } = [];
}

public class SerialMoveInfo
{
    public long InventoryTransactionLineId { get; set; }
    public long InventoryTransactionId { get; set; }
    public string TransactionType { get; set; } = null!;
    public DateTime TransactionDate { get; set; }
    public string? Reference { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? QtyDelta { get; set; }
}

public class ReservationResponse
{
    public long InventoryReservationId { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int InventoryItemDefinitionId { get; set; }
    public string? SkuCode { get; set; }
    public string? NameSnapshot { get; set; }
    public int Quantity { get; set; }
    public long? SaleOrderId { get; set; }
    public short? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<long>? ReservedSerialItemIds { get; set; }
}

public class SiteLookup
{
    public int SiteId { get; set; }
    public string Name { get; set; } = null!;
}

public class ItemDefinitionLookup
{
    public int InventoryItemDefinitionId { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? NameSnapshot { get; set; }
}
