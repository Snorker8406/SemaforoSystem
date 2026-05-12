using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Payables;

public class CreateProviderPayableLineRequest
{
    [Range(1, int.MaxValue)]
    public int LineNumber { get; set; }

    public long? PurchaseOrderLineId { get; set; }
    public long? PurchaseReceiptLineId { get; set; }
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }

    [Required]
    [StringLength(500)]
    public string DescriptionSnapshot { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    /// <summary>If null the API computes it as quantity * unit_cost - discount + tax.</summary>
    public decimal? LineTotal { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class CreateProviderPayableRequest
{
    [Range(1, int.MaxValue)]
    public int ProviderId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProviderPayableTypeId { get; set; }

    /// <summary>Optional. When omitted defaults to the OPEN status.</summary>
    [Range(1, int.MaxValue)]
    public int? ProviderPayableStatusId { get; set; }

    [Range(1, int.MaxValue)]
    public int SiteId { get; set; }

    [Range(1, int.MaxValue)]
    public int OpenedByEmployeeId { get; set; }

    public long? PurchaseOrderId { get; set; }
    public long? PurchaseReceiptId { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public DateTime? DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Required]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = "MXN";

    /// <summary>If null, computed from the lines.</summary>
    [Range(0, double.MaxValue)]
    public decimal? Subtotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TaxTotal { get; set; }

    /// <summary>If null, computed from the lines.</summary>
    [Range(0, double.MaxValue)]
    public decimal? Total { get; set; }

    public string? Notes { get; set; }

    public List<CreateProviderPayableLineRequest> Lines { get; set; } = new();
}

public class UpdateProviderPayableRequest
{
    [Range(1, int.MaxValue)]
    public int ProviderPayableTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProviderPayableStatusId { get; set; }

    public long? PurchaseOrderId { get; set; }
    public long? PurchaseReceiptId { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Required]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = "MXN";

    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }

    public string? Notes { get; set; }
}

public class UpdateProviderPayableStatusRequest
{
    [Range(1, int.MaxValue)]
    public int ProviderPayableStatusId { get; set; }

    public string? Comments { get; set; }
}

// ── Lines ───────────────────────────────────────────────

public class UpdateProviderPayableLineRequest : CreateProviderPayableLineRequest { }

// ── Transactions ────────────────────────────────────────

public class CreateProviderPayableTransactionRequest
{
    [Required]
    [StringLength(30)]
    public string TransactionType { get; set; } = string.Empty;

    public int? ProviderPaymentMethodId { get; set; }

    public DateTime? TransactionDate { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public string? Comments { get; set; }

    [Range(1, int.MaxValue)]
    public int CreatedByEmployeeId { get; set; }
}
