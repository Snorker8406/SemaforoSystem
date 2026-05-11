using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_orders")]
[Index("OrderDate", Name = "ix_purchase_orders_order_date")]
[Index("ProviderId", Name = "ix_purchase_orders_provider_id")]
[Index("SiteId", Name = "ix_purchase_orders_site_id")]
[Index("PurchaseOrderStatusId", Name = "ix_purchase_orders_status_id")]
public partial class PurchaseOrder
{
    [Key]
    [Column("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("purchase_order_status_id")]
    public int PurchaseOrderStatusId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("created_by_employee_id")]
    public int CreatedByEmployeeId { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("expected_delivery_date")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Column("folio")]
    [StringLength(50)]
    public string? Folio { get; set; }

    [Column("provider_reference")]
    [StringLength(100)]
    public string? ProviderReference { get; set; }

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

    [ForeignKey("CreatedByEmployeeId")]
    [InverseProperty("PurchaseOrders")]
    public virtual Employee CreatedByEmployee { get; set; } = null!;

    [ForeignKey("ProviderId")]
    [InverseProperty("PurchaseOrders")]
    public virtual Provider Provider { get; set; } = null!;

    [InverseProperty("PurchaseOrder")]
    public virtual ICollection<ProviderPayable> ProviderPayables { get; set; } = new List<ProviderPayable>();

    [InverseProperty("PurchaseOrder")]
    public virtual ICollection<PurchaseOrderExpense> PurchaseOrderExpenses { get; set; } = new List<PurchaseOrderExpense>();

    [InverseProperty("PurchaseOrder")]
    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    [ForeignKey("PurchaseOrderStatusId")]
    [InverseProperty("PurchaseOrders")]
    public virtual PurchaseOrderStatus PurchaseOrderStatus { get; set; } = null!;

    [InverseProperty("PurchaseOrder")]
    public virtual ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();

    [ForeignKey("SiteId")]
    [InverseProperty("PurchaseOrders")]
    public virtual Site Site { get; set; } = null!;
}
