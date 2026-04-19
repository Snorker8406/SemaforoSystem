using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("inventory_reservations")]
public partial class InventoryReservation
{
    [Key]
    [Column("inventory_reservation_id")]
    public long InventoryReservationId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("sale_order_id")]
    public long? SaleOrderId { get; set; }

    [Column("status")]
    public short? Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("InventoryReservations")]
    public virtual InventoryItemDefinition InventoryItemDefinition { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("InventoryReservations")]
    public virtual Site Site { get; set; } = null!;

    [ForeignKey("InventoryReservationId")]
    [InverseProperty("InventoryReservations")]
    public virtual ICollection<InventorySerialItem> InventorySerialItems { get; set; } = new List<InventorySerialItem>();
}
