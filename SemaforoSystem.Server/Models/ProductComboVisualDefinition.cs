using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combo_visual_definitions")]
[Index("IsActive", Name = "ix_combo_visual_definitions_active")]
[Index("ProductComboId", "SortOrder", Name = "ix_combo_visual_definitions_combo")]
public partial class ProductComboVisualDefinition
{
    [Key]
    [Column("product_combo_visual_definition_id")]
    public long ProductComboVisualDefinitionId { get; set; }

    [Column("product_combo_id")]
    public long ProductComboId { get; set; }

    [Column("name")]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("fixed_price_amount")]
    [Precision(12, 2)]
    public decimal? FixedPriceAmount { get; set; }

    [Column("discount_type")]
    [StringLength(20)]
    public string DiscountType { get; set; } = null!;

    [Column("discount_value")]
    [Precision(12, 2)]
    public decimal? DiscountValue { get; set; }

    [Column("price_list_id")]
    public int? PriceListId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("PriceListId")]
    [InverseProperty("ProductComboVisualDefinitions")]
    public virtual PriceList? PriceList { get; set; }

    [ForeignKey("ProductComboId")]
    [InverseProperty("ProductComboVisualDefinitions")]
    public virtual ProductCombo ProductCombo { get; set; } = null!;

    [InverseProperty("ProductComboVisualDefinition")]
    public virtual ICollection<ProductComboComponent> ProductComboComponents { get; set; } = new List<ProductComboComponent>();

    [InverseProperty("ProductComboVisualDefinition")]
    public virtual ProductComboImageTarget? ProductComboImageTarget { get; set; }

    [InverseProperty("ProductComboVisualDefinition")]
    public virtual ICollection<ProductComboVisualDefinitionSchool> ProductComboVisualDefinitionSchools { get; set; } = new List<ProductComboVisualDefinitionSchool>();

    [InverseProperty("ProductComboVisualDefinition")]
    public virtual ICollection<SalesLine> SalesLines { get; set; } = new List<SalesLine>();
}
