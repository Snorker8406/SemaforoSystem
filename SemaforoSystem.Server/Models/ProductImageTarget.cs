using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_image_targets")]
[Index("ProductImageId", Name = "ix_product_image_targets_image")]
[Index("InventoryItemDefinitionId", "SortOrder", Name = "ix_product_image_targets_item_definition")]
[Index("ProductId", "SortOrder", Name = "ix_product_image_targets_product")]
[Index("ProductVisualDefinitionId", "SortOrder", Name = "ix_product_image_targets_visual_def")]
public partial class ProductImageTarget
{
    [Key]
    [Column("product_image_target_id")]
    public long ProductImageTargetId { get; set; }

    [Column("product_image_id")]
    public long ProductImageId { get; set; }

    [Column("target_type")]
    [StringLength(30)]
    public string TargetType { get; set; } = null!;

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("ProductImageTarget")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductImageTarget")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductImageId")]
    [InverseProperty("ProductImageTargets")]
    public virtual ProductImage ProductImage { get; set; } = null!;

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductImageTarget")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }
}
