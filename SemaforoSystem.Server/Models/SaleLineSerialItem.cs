using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("SaleLineId", "InventorySerialItemId")]
[Table("sale_line_serial_items")]
[Index("InventorySerialItemId", Name = "ix_sale_line_serial_items_inventory_serial_item_id")]
[Index("InventorySerialItemId", Name = "uq_sale_line_serial_items_inventory_serial_item", IsUnique = true)]
public partial class SaleLineSerialItem
{
    [Key]
    [Column("sale_line_id")]
    public long SaleLineId { get; set; }

    [Key]
    [Column("inventory_serial_item_id")]
    public long InventorySerialItemId { get; set; }

    [ForeignKey("InventorySerialItemId")]
    [InverseProperty("SaleLineSerialItem")]
    public virtual InventorySerialItem InventorySerialItem { get; set; } = null!;

    [ForeignKey("SaleLineId")]
    [InverseProperty("SaleLineSerialItems")]
    public virtual SalesLine SaleLine { get; set; } = null!;
}
