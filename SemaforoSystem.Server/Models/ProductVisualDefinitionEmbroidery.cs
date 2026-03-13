using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductVisualDefinitionId", "EmbroideryId", "Placement")]
[Table("product_visual_definition_embroideries")]
[Index("EmbroideryId", Name = "ix_pvde_embroidery")]
[Index("ProductVisualDefinitionId", Name = "ix_pvde_visual_def")]
[Index("ProductVisualDefinitionId", "IsRequired", Name = "ix_pvde_visual_def_required")]
public partial class ProductVisualDefinitionEmbroidery
{
    [Key]
    [Column("product_visual_definition_id")]
    public long ProductVisualDefinitionId { get; set; }

    [Key]
    [Column("embroidery_id")]
    public int EmbroideryId { get; set; }

    [Key]
    [Column("placement")]
    [StringLength(50)]
    public string Placement { get; set; } = null!;

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("extra_price")]
    [Precision(12, 2)]
    public decimal? ExtraPrice { get; set; }

    [Column("notes")]
    [StringLength(200)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EmbroideryId")]
    [InverseProperty("ProductVisualDefinitionEmbroideries")]
    public virtual Embroidery Embroidery { get; set; } = null!;

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductVisualDefinitionEmbroideries")]
    public virtual ProductVisualDefinition ProductVisualDefinition { get; set; } = null!;
}
