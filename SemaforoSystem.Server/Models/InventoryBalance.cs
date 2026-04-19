using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("SiteId", "InventoryItemDefinitionId")]
[Table("inventory_balances")]
public partial class InventoryBalance
{
    [Key]
    [Column("site_id")]
    public int SiteId { get; set; }

    [Key]
    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("on_hand")]
    public int? OnHand { get; set; }

    [Column("reserved")]
    public int? Reserved { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("InventoryBalances")]
    public virtual InventoryItemDefinition InventoryItemDefinition { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("InventoryBalances")]
    public virtual Site Site { get; set; } = null!;
}
