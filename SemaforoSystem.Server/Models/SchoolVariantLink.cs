using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("school_variant_links")]
[Index("ProductVariantId", Name = "ux_school_variant_links_variant", IsUnique = true)]
public partial class SchoolVariantLink
{
    [Key]
    [Column("school_id")]
    public int SchoolId { get; set; }

    [Column("product_variant_id")]
    public int ProductVariantId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductVariantId")]
    [InverseProperty("SchoolVariantLink")]
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    [ForeignKey("SchoolId")]
    [InverseProperty("SchoolVariantLink")]
    public virtual School School { get; set; } = null!;
}
