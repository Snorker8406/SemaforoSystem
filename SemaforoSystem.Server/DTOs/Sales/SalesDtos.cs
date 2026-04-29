using System.ComponentModel.DataAnnotations;
using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Sales;

// ───────────────────────── Catalogs ─────────────────────────

public sealed class CatalogItemResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
}

// ───────────────────────── Query ────────────────────────────

public sealed class SaleQueryParameters : QueryParameters
{
    public int? SiteId { get; set; }
    public int? ClientId { get; set; }
    public int? EmployeeId { get; set; }
    public int? SaleTypeId { get; set; }
    public int? SaleStatusId { get; set; }
    public string? StatusCode { get; set; }
    public long? AccountId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// ───────────────────────── Responses ────────────────────────

public class SaleListItemResponse
{
    public long SaleId { get; set; }
    public string? Folio { get; set; }
    public DateTime SaleDate { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int SaleTypeId { get; set; }
    public string? SaleTypeCode { get; set; }
    public string? SaleTypeName { get; set; }
    public int SaleStatusId { get; set; }
    public string? SaleStatusCode { get; set; }
    public string? SaleStatusName { get; set; }
    public long? AccountId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal Balance { get; set; }
}

public sealed class SaleLineResponse
{
    public long SaleLineId { get; set; }
    public int LineNumber { get; set; }
    public string LineType { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public long? ProductComboVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public int? SizeId { get; set; }
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
    public int? LegacySaleDetailId { get; set; }
    public IList<long> SerialInventoryItemIds { get; set; } = new List<long>();
}

public sealed class SalePaymentResponse
{
    public long SalePaymentId { get; set; }
    public int PaymentMethodId { get; set; }
    public string? PaymentMethodCode { get; set; }
    public string? PaymentMethodName { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
}

public sealed class SaleDetailResponse : SaleListItemResponse
{
    public string? ExternalReference { get; set; }
    public int? LegacySaleId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IList<SaleLineResponse> Lines { get; set; } = new List<SaleLineResponse>();
    public IList<SalePaymentResponse> Payments { get; set; } = new List<SalePaymentResponse>();
}

// ───────────────────────── Requests ─────────────────────────

public sealed class CreateSaleLineRequest
{
    [Required, StringLength(30)]
    public string LineType { get; set; } = "PRODUCT";

    public int? LineNumber { get; set; }
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public long? ProductComboVisualDefinitionId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public int? SizeId { get; set; }

    [Required, StringLength(500)]
    public string DescriptionSnapshot { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal DiscountAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal TaxAmount { get; set; }

    /// <summary>Optional. If omitted, computed as (UnitPrice * Quantity - DiscountAmount + TaxAmount).</summary>
    public decimal? LineTotal { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public int? LegacySaleDetailId { get; set; }

    /// <summary>Optional serialized inventory items reserved by this line.</summary>
    public IList<long>? SerialInventoryItemIds { get; set; }
}

public sealed class CreateSalePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int PaymentMethodId { get; set; }

    public DateTime? PaymentDate { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}

public sealed class CreateSaleRequest
{
    [Range(1, int.MaxValue)]
    public int SaleTypeId { get; set; }

    /// <summary>Optional. If null, defaults to the status whose code is DRAFT.</summary>
    public int? SaleStatusId { get; set; }

    [Range(1, int.MaxValue)]
    public int SiteId { get; set; }

    public int? ClientId { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public long? AccountId { get; set; }

    public DateTime? SaleDate { get; set; }

    [StringLength(50)]
    public string? Folio { get; set; }

    [StringLength(100)]
    public string? ExternalReference { get; set; }

    public int? LegacySaleId { get; set; }

    public string? Notes { get; set; }

    [MinLength(1)]
    public IList<CreateSaleLineRequest> Lines { get; set; } = new List<CreateSaleLineRequest>();

    public IList<CreateSalePaymentRequest>? Payments { get; set; }

    /// <summary>If true, sale is immediately confirmed (inventory ledger generated, status -> COMPLETED).</summary>
    public bool Confirm { get; set; }
}

public sealed class UpdateSaleRequest
{
    public int? ClientId { get; set; }
    public long? AccountId { get; set; }

    [StringLength(50)]
    public string? Folio { get; set; }

    [StringLength(100)]
    public string? ExternalReference { get; set; }

    public string? Notes { get; set; }
}

public sealed class UpdateSaleStatusRequest
{
    /// <summary>Target status code (e.g. DRAFT, COMPLETED, CANCELED, VOID). Required.</summary>
    [Required, StringLength(50)]
    public string StatusCode { get; set; } = string.Empty;
}

public sealed class CancelSaleRequest
{
    [StringLength(500)]
    public string? Reason { get; set; }
}
