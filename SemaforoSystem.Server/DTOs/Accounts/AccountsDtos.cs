using System.ComponentModel.DataAnnotations;
using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Accounts;

// ───────────────────────── Catalogs ─────────────────────────

public sealed class AccountCatalogItemResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

// ───────────────────────── Query ────────────────────────────

public sealed class AccountQueryParameters : QueryParameters
{
    public int? ClientId { get; set; }
    public int? SiteId { get; set; }
    public int? AccountTypeId { get; set; }
    public int? AccountStatusId { get; set; }
    public string? TypeCode { get; set; }
    public string? StatusCode { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? Overdue { get; set; }
}

// ───────────────────────── Responses ────────────────────────

public class AccountListItemResponse
{
    public long AccountId { get; set; }
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public int AccountTypeId { get; set; }
    public string? AccountTypeCode { get; set; }
    public string? AccountTypeName { get; set; }
    public int AccountStatusId { get; set; }
    public string? AccountStatusCode { get; set; }
    public string? AccountStatusName { get; set; }
    public DateTime OpeningDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string CurrencyCode { get; set; } = "MXN";
    public string? Reference { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}

public sealed class AccountItemResponse
{
    public long AccountItemId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<long> SerialInventoryItemIds { get; set; } = new List<long>();
}

public sealed class AccountTransactionResponse
{
    public long AccountTransactionId { get; set; }
    public long AccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AccountInstallmentResponse
{
    public long AccountInstallmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreditAccountResponse
{
    public int? CreditDays { get; set; }
    public DateOnly? GraceUntil { get; set; }
    public DateOnly? OriginalDueDate { get; set; }
    public DateOnly? LastExtensionDate { get; set; }
    public DateOnly? ExtendedDueDate { get; set; }
    public decimal? CreditLimitSnapshot { get; set; }
    public bool RequiresGuarantor { get; set; }
}

public sealed class LayawayAccountResponse
{
    public decimal DepositAmount { get; set; }
    public DateOnly? ExpectedArrivalDate { get; set; }
    public DateOnly? ReadyDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public bool IsReady { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string? CancellationPolicyNotes { get; set; }
}

public sealed class AccountDetailResponse : AccountListItemResponse
{
    public int OpenedByEmployeeId { get; set; }
    public string? OpenedByEmployeeName { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime? CanceledDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IList<AccountItemResponse> Items { get; set; } = new List<AccountItemResponse>();
    public IList<AccountTransactionResponse> Transactions { get; set; } = new List<AccountTransactionResponse>();
    public IList<AccountInstallmentResponse> Installments { get; set; } = new List<AccountInstallmentResponse>();
    public CreditAccountResponse? Credit { get; set; }
    public LayawayAccountResponse? Layaway { get; set; }
}

// ───────────────────────── Requests ─────────────────────────

public sealed class CreateAccountItemRequest
{
    public int? InventoryItemDefinitionId { get; set; }
    public int? ProductId { get; set; }
    public long? ProductVisualDefinitionId { get; set; }

    [Required, StringLength(250)]
    public string DescriptionSnapshot { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal DiscountAmount { get; set; }

    /// <summary>Optional. If omitted, computed as (UnitPrice * Quantity - DiscountAmount).</summary>
    public decimal? LineTotal { get; set; }

    public IList<long>? SerialInventoryItemIds { get; set; }
}

public sealed class CreateCreditAccountRequest
{
    public int? CreditDays { get; set; }
    public DateOnly? GraceUntil { get; set; }
    public DateOnly? OriginalDueDate { get; set; }
    public DateOnly? ExtendedDueDate { get; set; }
    public decimal? CreditLimitSnapshot { get; set; }
    public bool RequiresGuarantor { get; set; }
}

public sealed class CreateLayawayAccountRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal DepositAmount { get; set; }

    public DateOnly? ExpectedArrivalDate { get; set; }
    public DateOnly? ReadyDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public bool IsReady { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    [StringLength(250)]
    public string? CancellationPolicyNotes { get; set; }
}

public sealed class CreateInstallmentRequest
{
    [Range(1, int.MaxValue)]
    public int InstallmentNumber { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal ExpectedAmount { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }
}

public sealed class UpdateInstallmentRequest
{
    public DateOnly? DueDate { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public bool? IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }
}

public sealed class CreateAccountRequest
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Range(1, int.MaxValue)]
    public int SiteId { get; set; }

    [Range(1, int.MaxValue)]
    public int OpenedByEmployeeId { get; set; }

    /// <summary>Explicit account type id, or null to resolve by <see cref="AccountTypeCode"/>.</summary>
    public int? AccountTypeId { get; set; }

    /// <summary>Catalog code (e.g. CREDIT, LAYAWAY). Used when <see cref="AccountTypeId"/> is null.</summary>
    [StringLength(30)]
    public string? AccountTypeCode { get; set; }

    public int? AccountStatusId { get; set; }

    /// <summary>Optional status code (defaults to OPEN).</summary>
    [StringLength(30)]
    public string? AccountStatusCode { get; set; }

    public DateTime? OpeningDate { get; set; }
    public DateTime? DueDate { get; set; }

    [StringLength(10)]
    public string? CurrencyCode { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public IList<CreateAccountItemRequest> Items { get; set; } = new List<CreateAccountItemRequest>();

    public CreateCreditAccountRequest? Credit { get; set; }
    public CreateLayawayAccountRequest? Layaway { get; set; }

    public IList<CreateInstallmentRequest>? Installments { get; set; }

    /// <summary>
    /// If true, automatically creates an initial CHARGE transaction for the total of items.
    /// Recommended per the guide (every debt/layaway should start with an explicit CHARGE).
    /// </summary>
    public bool GenerateInitialCharge { get; set; } = true;

    /// <summary>
    /// Optional initial payment amount captured at account opening. Generates a PAYMENT transaction.
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? InitialPaymentAmount { get; set; }

    [StringLength(30)]
    public string? InitialPaymentMethod { get; set; }
}

public sealed class UpdateAccountRequest
{
    public int? AccountStatusId { get; set; }
    [StringLength(30)]
    public string? AccountStatusCode { get; set; }
    public DateTime? DueDate { get; set; }
    [StringLength(10)]
    public string? CurrencyCode { get; set; }
    [StringLength(100)]
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateAccountStatusRequest
{
    [Required, StringLength(30)]
    public string StatusCode { get; set; } = string.Empty;
}

public sealed class CreateAccountTransactionRequest
{
    /// <summary>
    /// One of: CHARGE, PAYMENT, DISCOUNT, INTEREST, ADJUSTMENT, CANCELLATION.
    /// </summary>
    [Required, StringLength(30)]
    public string TransactionType { get; set; } = string.Empty;

    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Amount in absolute value. The controller will apply the sign based on TransactionType
    /// according to the guide's sign convention.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [StringLength(30)]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(2000)]
    public string? Comments { get; set; }

    [Range(1, int.MaxValue)]
    public int CreatedByEmployeeId { get; set; }
}

public sealed class CancelAccountRequest
{
    [StringLength(500)]
    public string? Reason { get; set; }

    [Range(1, int.MaxValue)]
    public int CanceledByEmployeeId { get; set; }
}

public sealed class UpdateLayawayDatesRequest
{
    public DateOnly? ExpectedArrivalDate { get; set; }
    public DateOnly? ReadyDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public bool? IsReady { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}
