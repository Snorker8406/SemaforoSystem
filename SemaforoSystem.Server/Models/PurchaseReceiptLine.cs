using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_receipt_lines")]
[Index("InventoryItemDefinitionId", Name = "ix_purchase_receipt_lines_inventory_item_definition_id")]
[Index("PurchaseOrderLineId", Name = "ix_purchase_receipt_lines_order_line_id")]
[Index("ProductId", Name = "ix_purchase_receipt_lines_product_id")]
[Index("PurchaseReceiptId", Name = "ix_purchase_receipt_lines_receipt_id")]
[Index("ProductVisualDefinitionId", Name = "ix_purchase_receipt_lines_visual_definition_id")]
[Index("PurchaseReceiptId", "LineNumber", Name = "uq_purchase_receipt_lines_receipt_number", IsUnique = true)]
public partial class PurchaseReceiptLine
{
    [Key]
    [Column("purchase_receipt_line_id")]
    public long PurchaseReceiptLineId { get; set; }

    [Column("purchase_receipt_id")]
    public long PurchaseReceiptId { get; set; }

    [Column("purchase_order_line_id")]
    public long? PurchaseOrderLineId { get; set; }

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

    [Column("description_snapshot")]
    [StringLength(500)]
    public string DescriptionSnapshot { get; set; } = null!;

    [Column("quantity_received")]
    [Precision(19, 4)]
    public decimal QuantityReceived { get; set; }

    [Column("quantity_accepted")]
    [Precision(19, 4)]
    public decimal QuantityAccepted { get; set; }

    [Column("quantity_rejected")]
    [Precision(19, 4)]
    public decimal QuantityRejected { get; set; }

    [Column("quantity_posted_to_inventory")]
    [Precision(19, 4)]
    public decimal QuantityPostedToInventory { get; set; }

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
    [InverseProperty("PurchaseReceiptLines")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [InverseProperty("PurchaseReceiptLine")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    [ForeignKey("ProductId")]
    [InverseProperty("PurchaseReceiptLines")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("PurchaseReceiptLines")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }

    [InverseProperty("PurchaseReceiptLine")]
    public virtual ICollection<ProviderPayableLine> ProviderPayableLines { get; set; } = new List<ProviderPayableLine>();

    [ForeignKey("PurchaseOrderLineId")]
    [InverseProperty("PurchaseReceiptLines")]
    public virtual PurchaseOrderLine? PurchaseOrderLine { get; set; }

    [ForeignKey("PurchaseReceiptId")]
    [InverseProperty("PurchaseReceiptLines")]
    public virtual PurchaseReceipt PurchaseReceipt { get; set; } = null!;
}
