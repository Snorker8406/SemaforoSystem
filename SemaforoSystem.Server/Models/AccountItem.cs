using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_items")]
[Index("AccountId", Name = "idx_account_items_account_id")]
[Index("InventoryItemDefinitionId", Name = "idx_account_items_inventory_item_definition_id")]
public partial class AccountItem
{
    [Key]
    [Column("account_item_id")]
    public long AccountItemId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [Column("description_snapshot")]
    [StringLength(250)]
    public string DescriptionSnapshot { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    [Precision(14, 2)]
    public decimal UnitPrice { get; set; }

    [Column("discount_amount")]
    [Precision(14, 2)]
    public decimal DiscountAmount { get; set; }

    [Column("line_total")]
    [Precision(14, 2)]
    public decimal LineTotal { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountItems")]
    public virtual Account Account { get; set; } = null!;

    [InverseProperty("AccountItem")]
    public virtual ICollection<AccountItemSerialItem> AccountItemSerialItems { get; set; } = new List<AccountItemSerialItem>();

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("AccountItems")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("AccountItems")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("AccountItems")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }
}
