using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("AccountItemId", "InventorySerialItemId")]
[Table("account_item_serial_items")]
[Index("InventorySerialItemId", Name = "account_item_serial_items_inventory_serial_item_id_key", IsUnique = true)]
[Index("InventorySerialItemId", Name = "idx_account_item_serial_items_inventory_serial_item_id")]
public partial class AccountItemSerialItem
{
    [Key]
    [Column("account_item_id")]
    public long AccountItemId { get; set; }

    [Key]
    [Column("inventory_serial_item_id")]
    public long InventorySerialItemId { get; set; }

    [ForeignKey("AccountItemId")]
    [InverseProperty("AccountItemSerialItems")]
    public virtual AccountItem AccountItem { get; set; } = null!;

    [ForeignKey("InventorySerialItemId")]
    [InverseProperty("AccountItemSerialItem")]
    public virtual InventorySerialItem InventorySerialItem { get; set; } = null!;
}
