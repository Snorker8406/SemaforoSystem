using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Stocks;

// ═══════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════

/// <summary>Full response for a stock entry (header) with lines and expenses.</summary>
public class StockEntryResponse
{
    public int StockEntryId { get; set; }
    public DateTime EntryDate { get; set; }
    public string UserId { get; set; } = null!;
    public string? Comments { get; set; }
    public int StockCount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal TotalExpenses { get; set; }
    public int TotalQuantity { get; set; }
    public List<StockLineResponse> Stocks { get; set; } = [];
    public List<StockExpenseResponse> Expenses { get; set; } = [];
}

/// <summary>Lightweight summary for stock entry lists.</summary>
public class StockEntrySummary
{
    public int StockEntryId { get; set; }
    public DateTime EntryDate { get; set; }
    public string UserId { get; set; } = null!;
    public string? Comments { get; set; }
    public int StockCount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal TotalExpenses { get; set; }
    public int TotalQuantity { get; set; }
}

/// <summary>Response for a single stock (inventory) line.</summary>
public class StockLineResponse
{
    public int StockId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? SizeId { get; set; }
    public string? SizeValue { get; set; }
    public int? VariantId { get; set; }
    public List<StockEmbroideryInfo> Embroideries { get; set; } = [];
    public int? Quantity { get; set; }
    public string Barcode { get; set; } = null!;
    public int? SerialNumber { get; set; }
    public decimal? PriceSpecial { get; set; }
    public int? PriceId { get; set; }
    public int? SaleDetailId { get; set; }
    public int? StockEntryId { get; set; }
    public DateTime? CreateDate { get; set; }
}

public class StockEmbroideryInfo
{
    public int EmbroideryId { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>Response for a stock expense line.</summary>
public class StockExpenseResponse
{
    public int StockExpenseId { get; set; }
    public int StockEntryId { get; set; }
    public decimal Amount { get; set; }
    public string? Comments { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Query parameters
// ═══════════════════════════════════════════════════════════════════

/// <summary>Query parameters for the stock entries list endpoint.</summary>
public class StockEntryQueryParameters : Common.QueryParameters
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
}

/// <summary>Query parameters for the stocks list endpoint.</summary>
public class StockQueryParameters : Common.QueryParameters
{
    public int? ProductId { get; set; }
    public int? SiteId { get; set; }
    public int? SizeId { get; set; }
    public int? EmbroideryId { get; set; }
    public int? StockEntryId { get; set; }
    public bool? HasQuantity { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – StockEntry (header)
// ═══════════════════════════════════════════════════════════════════

public class CreateStockEntryRequest
{
    public DateTime? EntryDate { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}

public class UpdateStockEntryRequest
{
    public DateTime? EntryDate { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – Stock (lines)
// ═══════════════════════════════════════════════════════════════════

public class CreateStockRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int SiteId { get; set; }

    public int? SizeId { get; set; }
    public int? VariantId { get; set; }
    public List<int> EmbroideryIds { get; set; } = [];
    public int? Quantity { get; set; }

    [StringLength(4)]
    public string? Barcode { get; set; }

    public int? SerialNumber { get; set; }
    public decimal? PriceSpecial { get; set; }
    public int? PriceId { get; set; }
}

public class UpdateStockRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int SiteId { get; set; }

    public int? SizeId { get; set; }
    public int? VariantId { get; set; }
    public List<int> EmbroideryIds { get; set; } = [];
    public int? Quantity { get; set; }

    [StringLength(4)]
    public string? Barcode { get; set; }

    public int? SerialNumber { get; set; }
    public decimal? PriceSpecial { get; set; }
    public int? PriceId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Create / Update requests – StockExpense
// ═══════════════════════════════════════════════════════════════════

public class CreateStockExpenseRequest
{
    [Required]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}

public class UpdateStockExpenseRequest
{
    [Required]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}
