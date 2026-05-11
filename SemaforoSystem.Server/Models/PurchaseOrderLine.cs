using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_order_lines")]
[Index("InventoryItemDefinitionId", Name = "ix_purchase_order_lines_inventory_item_definition_id")]
[Index("PurchaseOrderId", Name = "ix_purchase_order_lines_order_id")]
[Index("ProductId", Name = "ix_purchase_order_lines_product_id")]
[Index("ProductVisualDefinitionId", Name = "ix_purchase_order_lines_visual_definition_id")]
[Index("PurchaseOrderId", "LineNumber", Name = "uq_purchase_order_lines_order_number", IsUnique = true)]
public partial class PurchaseOrderLine
{
    [Key]
    [Column("purchase_order_line_id")]
    public long PurchaseOrderLineId { get; set; }

    [Column("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("line_type")]
    [StringLength(40)]
    public string LineType { get; set; } = null!;

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [Column("provider_id")]
    public int? ProviderId { get; set; }

    [Column("description_snapshot")]
    [StringLength(500)]
    public string DescriptionSnapshot { get; set; } = null!;

    [Column("quantity_ordered")]
    [Precision(19, 4)]
    public decimal QuantityOrdered { get; set; }

    [Column("unit_cost")]
    [Precision(19, 4)]
    public decimal UnitCost { get; set; }

    [Column("discount_amount")]
    [Precision(19, 4)]
    public decimal DiscountAmount { get; set; }

    [Column("tax_amount")]
    [Precision(19, 4)]
    public decimal TaxAmount { get; set; }

    [Column("line_total")]
    [Precision(19, 4)]
    public decimal LineTotal { get; set; }

    [Column("notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("PurchaseOrderLines")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("PurchaseOrderLines")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("PurchaseOrderLines")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("PurchaseOrderLines")]
    public virtual Provider? Provider { get; set; }

    [InverseProperty("PurchaseOrderLine")]
    public virtual ICollection<ProviderPayableLine> ProviderPayableLines { get; set; } = new List<ProviderPayableLine>();

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseOrderLines")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    [InverseProperty("PurchaseOrderLine")]
    public virtual ICollection<PurchaseReceiptLine> PurchaseReceiptLines { get; set; } = new List<PurchaseReceiptLine>();
}
