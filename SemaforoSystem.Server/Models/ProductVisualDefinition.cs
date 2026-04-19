using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_visual_definitions")]
[Index("ProductId", Name = "ix_product_visual_definitions_product")]
[Index("ProductId", "VariantsHash", Name = "ux_product_visual_definitions_product_hash", IsUnique = true)]
public partial class ProductVisualDefinition
{
    [Key]
    [Column("product_visual_definition_id")]
    public long ProductVisualDefinitionId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("variants_hash")]
    [StringLength(64)]
    public string VariantsHash { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("ProductVisualDefinition")]
    public virtual ICollection<InventoryItemDefinition> InventoryItemDefinitions { get; set; } = new List<InventoryItemDefinition>();

    [InverseProperty("ProductVisualDefinition")]
    public virtual ICollection<PriceVisualDefinitionEntry> PriceVisualDefinitionEntries { get; set; } = new List<PriceVisualDefinitionEntry>();

    [ForeignKey("ProductId")]
    [InverseProperty("ProductVisualDefinitions")]
    public virtual Product Product { get; set; } = null!;

    [InverseProperty("ProductVisualDefinition")]
    public virtual ICollection<ProductComboComponent> ProductComboComponents { get; set; } = new List<ProductComboComponent>();

    [InverseProperty("ProductVisualDefinition")]
    public virtual ProductImageTarget? ProductImageTarget { get; set; }

    [InverseProperty("ProductVisualDefinition")]
    public virtual ICollection<ProductVisualDefinitionEmbroidery> ProductVisualDefinitionEmbroideries { get; set; } = new List<ProductVisualDefinitionEmbroidery>();

    [InverseProperty("ProductVisualDefinition")]
    public virtual ICollection<ProductVisualDefinitionSchool> ProductVisualDefinitionSchools { get; set; } = new List<ProductVisualDefinitionSchool>();

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductVisualDefinitions")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
