using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("price_item_definition_entries")]
[Index("PriceListId", "InventoryItemDefinitionId", "ValidFrom", Name = "ix_pid_price_lookup", IsDescending = new[] { false, false, true })]
[Index("ValidFrom", "ValidTo", Name = "ix_pid_price_validity")]
public partial class PriceItemDefinitionEntry
{
    [Key]
    [Column("price_entry_id")]
    public long PriceEntryId { get; set; }

    [Column("price_list_id")]
    public int PriceListId { get; set; }

    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("price_amount")]
    [Precision(12, 2)]
    public decimal PriceAmount { get; set; }

    [Column("price_kind")]
    [StringLength(20)]
    public string PriceKind { get; set; } = null!;

    [Column("priority")]
    public int Priority { get; set; }

    [Column("promo_name")]
    [StringLength(120)]
    public string? PromoName { get; set; }

    [Column("promo_code")]
    [StringLength(60)]
    public string? PromoCode { get; set; }

    [Column("valid_from")]
    public DateTime ValidFrom { get; set; }

    [Column("valid_to")]
    public DateTime? ValidTo { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(200)]
    public string? CreatedBy { get; set; }

    [Column("reason")]
    [StringLength(200)]
    public string? Reason { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("PriceItemDefinitionEntries")]
    public virtual InventoryItemDefinition InventoryItemDefinition { get; set; } = null!;

    [ForeignKey("PriceListId")]
    [InverseProperty("PriceItemDefinitionEntries")]
    public virtual PriceList PriceList { get; set; } = null!;
}
