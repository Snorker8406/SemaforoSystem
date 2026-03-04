using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("inventory_transaction_lines")]
public partial class InventoryTransactionLine
{
    [Key]
    [Column("inventory_transaction_line_id")]
    public long InventoryTransactionLineId { get; set; }

    [Column("inventory_transaction_id")]
    public long InventoryTransactionId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("qty_delta")]
    public int? QtyDelta { get; set; }

    [Column("unit_cost")]
    [Precision(12, 2)]
    public decimal? UnitCost { get; set; }

    [Column("unit_price")]
    [Precision(12, 2)]
    public decimal? UnitPrice { get; set; }

    [Column("sale_detail_id")]
    public int? SaleDetailId { get; set; }

    [Column("source_site_id")]
    public int? SourceSiteId { get; set; }

    [Column("target_site_id")]
    public int? TargetSiteId { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("InventoryTransactionLines")]
    public virtual InventoryItemDefinition InventoryItemDefinition { get; set; } = null!;

    [ForeignKey("InventoryTransactionId")]
    [InverseProperty("InventoryTransactionLines")]
    public virtual InventoryTransaction InventoryTransaction { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("InventoryTransactionLineSites")]
    public virtual Site Site { get; set; } = null!;

    [ForeignKey("SourceSiteId")]
    [InverseProperty("InventoryTransactionLineSourceSites")]
    public virtual Site? SourceSite { get; set; }

    [ForeignKey("TargetSiteId")]
    [InverseProperty("InventoryTransactionLineTargetSites")]
    public virtual Site? TargetSite { get; set; }

    [ForeignKey("InventoryTransactionLineId")]
    [InverseProperty("InventoryTransactionLines")]
    public virtual ICollection<InventorySerialItem> InventorySerialItems { get; set; } = new List<InventorySerialItem>();
}
