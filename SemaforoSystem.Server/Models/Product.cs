using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("products")]
public partial class Product
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("brand_id")]
    public int? BrandId { get; set; }

    [Column("product_picture_id")]
    public int? ProductPictureId { get; set; }

    [Column("name")]
    [StringLength(500)]
    public string? Name { get; set; }

    [Column("create_date")]
    public DateTime? CreateDate { get; set; }

    [Column("barcode")]
    [StringLength(50)]
    public string? Barcode { get; set; }

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("model")]
    [StringLength(50)]
    public string? Model { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("serial_count")]
    public long? SerialCount { get; set; }

    [Column("serialize")]
    public bool? Serialize { get; set; }

    [ForeignKey("BrandId")]
    [InverseProperty("Products")]
    public virtual Brand? Brand { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<ProductComboDetail> ProductComboDetails { get; set; } = new List<ProductComboDetail>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductCost> ProductCosts { get; set; } = new List<ProductCost>();

    [ForeignKey("ProductPictureId")]
    [InverseProperty("Products")]
    public virtual ProductPicture? ProductPicture { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<ProductPicture> ProductPictures { get; set; } = new List<ProductPicture>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    [InverseProperty("Product")]
    public virtual ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();

    [InverseProperty("Product")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    [ForeignKey("ProductId")]
    [InverseProperty("Products")]
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    [ForeignKey("ProductId")]
    [InverseProperty("Products")]
    public virtual ICollection<ProductVariantSystem> ProductVariantSystems { get; set; } = new List<ProductVariantSystem>();

    [ForeignKey("ProductId")]
    [InverseProperty("Products")]
    public virtual ICollection<School> Schools { get; set; } = new List<School>();
}
