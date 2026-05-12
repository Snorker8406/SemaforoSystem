namespace SemaforoSystem.Server.DTOs.Payables;

/// <summary>Catalog item (type / status / payment method) used by payable endpoints.</summary>
public class PayableCatalogItemResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Compact list-item representation of a payable.</summary>
public class ProviderPayableListItemResponse
{
    public long ProviderPayableId { get; set; }

    public int ProviderId { get; set; }
    public string ProviderLegalName { get; set; } = string.Empty;
    public string? ProviderTradeName { get; set; }

    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;

    public int ProviderPayableTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;

    public int ProviderPayableStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }
    public string? Reference { get; set; }
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>Sum of CHARGE-type transactions (defaults to Total when no ledger exists).</summary>
    public decimal ChargedAmount { get; set; }

    /// <summary>Sum of payments + discounts + credit notes (positive value).</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Outstanding balance derived from the ledger.</summary>
    public decimal Balance { get; set; }

    public long? PurchaseOrderId { get; set; }
    public long? PurchaseReceiptId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProviderPayableLineResponse
{
    public long ProviderPayableLineId { get; set; }
    public long ProviderPayableId { get; set; }
    public int LineNumber { get; set; }

    public long? PurchaseOrderLineId { get; set; }
    public long? PurchaseReceiptLineId { get; set; }
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }

    public string DescriptionSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class ProviderPayableTransactionResponse
{
    public long ProviderPayableTransactionId { get; set; }
    public long ProviderPayableId { get; set; }
    public string TransactionType { get; set; } = string.Empty;

    public int? ProviderPaymentMethodId { get; set; }
    public string? ProviderPaymentMethodCode { get; set; }
    public string? ProviderPaymentMethodName { get; set; }

    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProviderPayableDetailResponse : ProviderPayableListItemResponse
{
    public int OpenedByEmployeeId { get; set; }
    public string? OpenedByEmployeeName { get; set; }
    public string? Notes { get; set; }

    public List<ProviderPayableLineResponse> Lines { get; set; } = new();
    public List<ProviderPayableTransactionResponse> Transactions { get; set; } = new();
}
