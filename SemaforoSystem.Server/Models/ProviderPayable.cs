using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payables")]
[Index("DocumentDate", Name = "ix_provider_payables_document_date")]
[Index("DueDate", Name = "ix_provider_payables_due_date")]
[Index("ProviderId", Name = "ix_provider_payables_provider_id")]
[Index("PurchaseOrderId", Name = "ix_provider_payables_purchase_order_id")]
[Index("PurchaseReceiptId", Name = "ix_provider_payables_purchase_receipt_id")]
[Index("ProviderPayableStatusId", Name = "ix_provider_payables_status_id")]
[Index("ProviderPayableTypeId", Name = "ix_provider_payables_type_id")]
public partial class ProviderPayable
{
    [Key]
    [Column("provider_payable_id")]
    public long ProviderPayableId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("provider_payable_type_id")]
    public int ProviderPayableTypeId { get; set; }

    [Column("provider_payable_status_id")]
    public int ProviderPayableStatusId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("opened_by_employee_id")]
    public int OpenedByEmployeeId { get; set; }

    [Column("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [Column("purchase_receipt_id")]
    public long? PurchaseReceiptId { get; set; }

    [Column("document_number")]
    [StringLength(100)]
    public string? DocumentNumber { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("document_date")]
    public DateTime DocumentDate { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("currency_code")]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = null!;

    [Column("subtotal")]
    [Precision(19, 4)]
    public decimal Subtotal { get; set; }

    [Column("discount_total")]
    [Precision(19, 4)]
    public decimal DiscountTotal { get; set; }

    [Column("tax_total")]
    [Precision(19, 4)]
    public decimal TaxTotal { get; set; }

    [Column("total")]
    [Precision(19, 4)]
    public decimal Total { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("OpenedByEmployeeId")]
    [InverseProperty("ProviderPayables")]
    public virtual Employee OpenedByEmployee { get; set; } = null!;

    [ForeignKey("ProviderId")]
    [InverseProperty("ProviderPayables")]
    public virtual Provider Provider { get; set; } = null!;

    [InverseProperty("ProviderPayable")]
    public virtual ICollection<ProviderPayableLine> ProviderPayableLines { get; set; } = new List<ProviderPayableLine>();

    [ForeignKey("ProviderPayableStatusId")]
    [InverseProperty("ProviderPayables")]
    public virtual ProviderPayableStatus ProviderPayableStatus { get; set; } = null!;

    [InverseProperty("ProviderPayable")]
    public virtual ICollection<ProviderPayableTransaction> ProviderPayableTransactions { get; set; } = new List<ProviderPayableTransaction>();

    [ForeignKey("ProviderPayableTypeId")]
    [InverseProperty("ProviderPayables")]
    public virtual ProviderPayableType ProviderPayableType { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("ProviderPayables")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [InverseProperty("ProviderPayable")]
    public virtual ICollection<PurchaseOrderExpense> PurchaseOrderExpenses { get; set; } = new List<PurchaseOrderExpense>();

    [ForeignKey("PurchaseReceiptId")]
    [InverseProperty("ProviderPayables")]
    public virtual PurchaseReceipt? PurchaseReceipt { get; set; }

    [ForeignKey("SiteId")]
    [InverseProperty("ProviderPayables")]
    public virtual Site Site { get; set; } = null!;
}
