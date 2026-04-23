using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("inventory_serial_items")]
[Index("Barcode", Name = "inventory_serial_items_barcode_unique", IsUnique = true)]
public partial class InventorySerialItem
{
    [Key]
    [Column("inventory_serial_item_id")]
    public long InventorySerialItemId { get; set; }

    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("current_site_id")]
    public int CurrentSiteId { get; set; }

    [Column("barcode")]
    [StringLength(64)]
    public string Barcode { get; set; } = null!;

    [Column("serial_number")]
    public long? SerialNumber { get; set; }

    [Column("status")]
    public short Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("deactivated_at")]
    public DateTime? DeactivatedAt { get; set; }

    [InverseProperty("InventorySerialItem")]
    public virtual AccountItemSerialItem? AccountItemSerialItem { get; set; }

    [ForeignKey("CurrentSiteId")]
    [InverseProperty("InventorySerialItems")]
    public virtual Site CurrentSite { get; set; } = null!;

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("InventorySerialItems")]
    public virtual InventoryItemDefinition InventoryItemDefinition { get; set; } = null!;

    [ForeignKey("InventorySerialItemId")]
    [InverseProperty("InventorySerialItems")]
    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    [ForeignKey("InventorySerialItemId")]
    [InverseProperty("InventorySerialItems")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();
}
