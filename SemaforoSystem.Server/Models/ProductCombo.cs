using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combos")]
[Index("IsActive", Name = "ix_product_combos_active")]
public partial class ProductCombo
{
    [Key]
    [Column("product_combo_id")]
    public long ProductComboId { get; set; }

    [Column("name")]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("ProductCombo")]
    public virtual ICollection<ProductComboVisualDefinition> ProductComboVisualDefinitions { get; set; } = new List<ProductComboVisualDefinition>();
}
