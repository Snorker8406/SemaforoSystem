using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("inventory_item_definitions")]
[Index("SkuCode", Name = "inventory_item_definitions_sku_code_unique", IsUnique = true)]
[Index("ProductVisualDefinitionId", Name = "ix_inventory_item_definitions_visual_def")]
public partial class InventoryItemDefinition
{
    [Key]
    [Column("inventory_item_definition_id")]
    public int InventoryItemDefinitionId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("size_id")]
    public int? SizeId { get; set; }

    [Column("is_serialized")]
    public bool IsSerialized { get; set; }

    [Column("sku_code")]
    [StringLength(300)]
    public string SkuCode { get; set; } = null!;

    [Column("name_snapshot")]
    [StringLength(500)]
    public string? NameSnapshot { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<AccountItem> AccountItems { get; set; } = new List<AccountItem>();

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<InventorySerialItem> InventorySerialItems { get; set; } = new List<InventorySerialItem>();

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<PriceItemDefinitionEntry> PriceItemDefinitionEntries { get; set; } = new List<PriceItemDefinitionEntry>();

    [ForeignKey("ProductId")]
    [InverseProperty("InventoryItemDefinitions")]
    public virtual Product Product { get; set; } = null!;

    [InverseProperty("InventoryItemDefinition")]
    public virtual ProductImageTarget? ProductImageTarget { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("InventoryItemDefinitions")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }

    [InverseProperty("InventoryItemDefinition")]
    public virtual ICollection<SalesLine> SalesLines { get; set; } = new List<SalesLine>();

    [ForeignKey("SizeId")]
    [InverseProperty("InventoryItemDefinitions")]
    public virtual Size? Size { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("InventoryItemDefinitions")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
