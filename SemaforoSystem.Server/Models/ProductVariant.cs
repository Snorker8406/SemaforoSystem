using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_variants")]
public partial class ProductVariant
{
    [Key]
    [Column("product_variant_id")]
    public int ProductVariantId { get; set; }

    [Column("product_variant_system_id")]
    public int ProductVariantSystemId { get; set; }

    [Column("variant_value")]
    [StringLength(150)]
    public string VariantValue { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [ForeignKey("ProductVariantSystemId")]
    [InverseProperty("ProductVariants")]
    public virtual ProductVariantSystem ProductVariantSystem { get; set; } = null!;
}
