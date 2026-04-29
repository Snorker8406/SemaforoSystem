using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sales_lines")]
[Index("InventoryItemDefinitionId", Name = "ix_sales_lines_inventory_item_definition_id")]
[Index("LegacySaleDetailId", Name = "ix_sales_lines_legacy_sale_detail_id")]
[Index("ProductComboVisualDefinitionId", Name = "ix_sales_lines_product_combo_visual_definition_id")]
[Index("ProductId", Name = "ix_sales_lines_product_id")]
[Index("ProductVisualDefinitionId", Name = "ix_sales_lines_product_visual_definition_id")]
[Index("SaleId", Name = "ix_sales_lines_sale_id")]
[Index("SizeId", Name = "ix_sales_lines_size_id")]
[Index("SaleId", "LineNumber", Name = "uq_sales_lines_sale_line_number", IsUnique = true)]
public partial class SalesLine
{
    [Key]
    [Column("sale_line_id")]
    public long SaleLineId { get; set; }

    [Column("sale_id")]
    public long SaleId { get; set; }

    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("line_type")]
    [StringLength(30)]
    public string LineType { get; set; } = null!;

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [Column("product_combo_visual_definition_id")]
    public long? ProductComboVisualDefinitionId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [Column("size_id")]
    public int? SizeId { get; set; }

    [Column("description_snapshot")]
    [StringLength(500)]
    public string DescriptionSnapshot { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    [Precision(19, 4)]
    public decimal UnitPrice { get; set; }

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

    [Column("legacy_sale_detail_id")]
    public int? LegacySaleDetailId { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("SalesLines")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [InverseProperty("SaleLine")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    [ForeignKey("ProductId")]
    [InverseProperty("SalesLines")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductComboVisualDefinitionId")]
    [InverseProperty("SalesLines")]
    public virtual ProductComboVisualDefinition? ProductComboVisualDefinition { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("SalesLines")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }

    [ForeignKey("SaleId")]
    [InverseProperty("SalesLines")]
    public virtual Sale Sale { get; set; } = null!;

    [InverseProperty("SaleLine")]
    public virtual ICollection<SaleLineSerialItem> SaleLineSerialItems { get; set; } = new List<SaleLineSerialItem>();

    [ForeignKey("SizeId")]
    [InverseProperty("SalesLines")]
    public virtual Size? Size { get; set; }
}
