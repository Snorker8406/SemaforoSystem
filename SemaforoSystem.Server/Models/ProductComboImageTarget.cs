using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combo_image_targets")]
[Index("ProductComboVisualDefinitionId", "SortOrder", Name = "ix_combo_image_targets_visual_def")]
[Index("ProductComboImageId", "ProductComboVisualDefinitionId", Name = "ux_combo_image_targets_unique", IsUnique = true)]
public partial class ProductComboImageTarget
{
    [Key]
    [Column("product_combo_image_target_id")]
    public long ProductComboImageTargetId { get; set; }

    [Column("product_combo_image_id")]
    public long ProductComboImageId { get; set; }

    [Column("product_combo_visual_definition_id")]
    public long ProductComboVisualDefinitionId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductComboImageId")]
    [InverseProperty("ProductComboImageTargets")]
    public virtual ProductComboImage ProductComboImage { get; set; } = null!;

    [ForeignKey("ProductComboVisualDefinitionId")]
    [InverseProperty("ProductComboImageTarget")]
    public virtual ProductComboVisualDefinition ProductComboVisualDefinition { get; set; } = null!;
}
