using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sales_details")]
public partial class SalesDetail
{
    [Key]
    [Column("sale_detail_id")]
    public int SaleDetailId { get; set; }

    [Column("sale_id")]
    public int SaleId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("size_id")]
    public int? SizeId { get; set; }

    [Column("price")]
    [Precision(19, 4)]
    public decimal Price { get; set; }

    [Column("special_price")]
    [Precision(19, 4)]
    public decimal? SpecialPrice { get; set; }

    [Column("barcode")]
    [StringLength(100)]
    public string Barcode { get; set; } = null!;

    [Column("delivered")]
    public bool Delivered { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("SalesDetails")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SaleId")]
    [InverseProperty("SalesDetails")]
    public virtual Sale Sale { get; set; } = null!;

    [ForeignKey("SizeId")]
    [InverseProperty("SalesDetails")]
    public virtual Size? Size { get; set; }

    [InverseProperty("SaleDetail")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
