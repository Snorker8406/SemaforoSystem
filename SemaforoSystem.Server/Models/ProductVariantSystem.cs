using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_variant_system")]
[Index("Name", Name = "variant_system_name_unique", IsUnique = true)]
public partial class ProductVariantSystem
{
    [Key]
    [Column("product_variant_id")]
    public int ProductVariantId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("label")]
    [StringLength(50)]
    public string? Label { get; set; }

    [InverseProperty("ProductVariantSystem")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    [ForeignKey("ProductVariantSystemId")]
    [InverseProperty("ProductVariantSystems")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
