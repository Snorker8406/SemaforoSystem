using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductVisualDefinitionId", "SchoolId")]
[Table("product_visual_definition_schools")]
[Index("SchoolId", Name = "ix_pvds_school")]
[Index("ProductVisualDefinitionId", Name = "ix_pvds_visual_definition")]
public partial class ProductVisualDefinitionSchool
{
    [Key]
    [Column("product_visual_definition_id")]
    public long ProductVisualDefinitionId { get; set; }

    [Key]
    [Column("school_id")]
    public int SchoolId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductVisualDefinitionSchools")]
    public virtual ProductVisualDefinition ProductVisualDefinition { get; set; } = null!;

    [ForeignKey("SchoolId")]
    [InverseProperty("ProductVisualDefinitionSchools")]
    public virtual School School { get; set; } = null!;
}
