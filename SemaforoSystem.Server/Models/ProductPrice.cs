using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_prices")]
[Index("VariantId", Name = "fki_product_prices_variant_id_fkey")]
public partial class ProductPrice
{
    [Key]
    [Column("price_id")]
    public int PriceId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("size_id")]
    public int? SizeId { get; set; }

    [Column("price")]
    [Precision(19, 4)]
    public decimal Price { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime CreateDate { get; set; }

    [Column("variant_id")]
    public int? VariantId { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductPrices")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SizeId")]
    [InverseProperty("ProductPrices")]
    public virtual Size? Size { get; set; }

    [ForeignKey("VariantId")]
    [InverseProperty("ProductPrices")]
    public virtual ProductVariant? Variant { get; set; }
}
