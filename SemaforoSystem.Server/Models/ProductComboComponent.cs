using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combo_components")]
[Index("ProductComboVisualDefinitionId", "SortOrder", Name = "ix_combo_components_visual_def")]
public partial class ProductComboComponent
{
    [Key]
    [Column("product_combo_component_id")]
    public long ProductComboComponentId { get; set; }

    [Column("product_combo_visual_definition_id")]
    public long ProductComboVisualDefinitionId { get; set; }

    [Column("component_type")]
    [StringLength(40)]
    public string ComponentType { get; set; } = null!;

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("product_visual_definition_id")]
    public long? ProductVisualDefinitionId { get; set; }

    [Column("embroidery_id")]
    public int? EmbroideryId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("placement")]
    [StringLength(50)]
    public string? Placement { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("extra_price")]
    [Precision(12, 2)]
    public decimal? ExtraPrice { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EmbroideryId")]
    [InverseProperty("ProductComboComponents")]
    public virtual Embroidery? Embroidery { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductComboComponents")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductComboVisualDefinitionId")]
    [InverseProperty("ProductComboComponents")]
    public virtual ProductComboVisualDefinition ProductComboVisualDefinition { get; set; } = null!;

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductComboComponents")]
    public virtual ProductVisualDefinition? ProductVisualDefinition { get; set; }
}
