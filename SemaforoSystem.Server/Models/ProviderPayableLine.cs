using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payable_lines")]
[Index("ProviderPayableId", Name = "ix_provider_payable_lines_payable_id")]
[Index("ProviderPayableId", "LineNumber", Name = "uq_provider_payable_lines_payable_number", IsUnique = true)]
public partial class ProviderPayableLine
{
    [Key]
    [Column("provider_payable_line_id")]
    public long ProviderPayableLineId { get; set; }

    [Column("provider_payable_id")]
    public long ProviderPayableId { get; set; }

    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("purchase_order_line_id")]
    public long? PurchaseOrderLineId { get; set; }

    [Column("purchase_receipt_line_id")]
    public long? PurchaseReceiptLineId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [Column("description_snapshot")]
    [StringLength(500)]
    public string DescriptionSnapshot { get; set; } = null!;

    [Column("quantity")]
    [Precision(19, 4)]
    public decimal Quantity { get; set; }

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
    [InverseProperty("ProviderPayableLines")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProviderPayableLines")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProviderPayableLines")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }

    [ForeignKey("ProviderPayableId")]
    [InverseProperty("ProviderPayableLines")]
    public virtual ProviderPayable ProviderPayable { get; set; } = null!;

    [ForeignKey("PurchaseOrderLineId")]
    [InverseProperty("ProviderPayableLines")]
    public virtual PurchaseOrderLine? PurchaseOrderLine { get; set; }

    [ForeignKey("PurchaseReceiptLineId")]
    [InverseProperty("ProviderPayableLines")]
    public virtual PurchaseReceiptLine? PurchaseReceiptLine { get; set; }
}
