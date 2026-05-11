using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_receipts")]
[Index("PurchaseOrderId", Name = "ix_purchase_receipts_order_id")]
[Index("ProviderId", Name = "ix_purchase_receipts_provider_id")]
[Index("ReceiptDate", Name = "ix_purchase_receipts_receipt_date")]
[Index("PurchaseReceiptStatusId", Name = "ix_purchase_receipts_status_id")]
public partial class PurchaseReceipt
{
    [Key]
    [Column("purchase_receipt_id")]
    public long PurchaseReceiptId { get; set; }

    [Column("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("purchase_receipt_status_id")]
    public int PurchaseReceiptStatusId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("received_by_employee_id")]
    public int ReceivedByEmployeeId { get; set; }

    [Column("receipt_date")]
    public DateTime ReceiptDate { get; set; }

    [Column("provider_document_number")]
    [StringLength(100)]
    public string? ProviderDocumentNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("PurchaseReceipt")]
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    [ForeignKey("ProviderId")]
    [InverseProperty("PurchaseReceipts")]
    public virtual Provider Provider { get; set; } = null!;

    [InverseProperty("PurchaseReceipt")]
    public virtual ICollection<ProviderPayable> ProviderPayables { get; set; } = new List<ProviderPayable>();

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseReceipts")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [InverseProperty("PurchaseReceipt")]
    public virtual ICollection<PurchaseReceiptLine> PurchaseReceiptLines { get; set; } = new List<PurchaseReceiptLine>();

    [ForeignKey("PurchaseReceiptStatusId")]
    [InverseProperty("PurchaseReceipts")]
    public virtual PurchaseReceiptStatus PurchaseReceiptStatus { get; set; } = null!;

    [ForeignKey("ReceivedByEmployeeId")]
    [InverseProperty("PurchaseReceipts")]
    public virtual Employee ReceivedByEmployee { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("PurchaseReceipts")]
    public virtual Site Site { get; set; } = null!;
}
