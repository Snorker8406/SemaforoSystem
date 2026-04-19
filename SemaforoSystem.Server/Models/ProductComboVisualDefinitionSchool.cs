using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductComboVisualDefinitionId", "SchoolId")]
[Table("product_combo_visual_definition_schools")]
[Index("ProductComboVisualDefinitionId", Name = "ix_pcvds_combo_visual_definition")]
[Index("SchoolId", Name = "ix_pcvds_school")]
public partial class ProductComboVisualDefinitionSchool
{
    [Key]
    [Column("product_combo_visual_definition_id")]
    public long ProductComboVisualDefinitionId { get; set; }

    [Key]
    [Column("school_id")]
    public int SchoolId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductComboVisualDefinitionId")]
    [InverseProperty("ProductComboVisualDefinitionSchools")]
    public virtual ProductComboVisualDefinition ProductComboVisualDefinition { get; set; } = null!;

    [ForeignKey("SchoolId")]
    [InverseProperty("ProductComboVisualDefinitionSchools")]
    public virtual School School { get; set; } = null!;
}
