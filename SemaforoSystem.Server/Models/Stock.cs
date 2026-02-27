using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("stocks")]
[Index("StockEntryId", Name = "fki_stock_stock_entry_id_fkey")]
public partial class Stock
{
    [Key]
    [Column("stock_id")]
    public int StockId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("size_id")]
    public int? SizeId { get; set; }

    [Column("sale_detail_id")]
    public int? SaleDetailId { get; set; }

    [Column("price_special", TypeName = "money")]
    public decimal? PriceSpecial { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime? CreateDate { get; set; }

    [Column("serial_number")]
    public int? SerialNumber { get; set; }

    [Column("barcode")]
    [StringLength(4)]
    public string Barcode { get; set; } = null!;

    [Column("stock_entry_id")]
    public int? StockEntryId { get; set; }

    [Column("variant_id")]
    public int? VariantId { get; set; }

    [Column("price_id")]
    public int? PriceId { get; set; }

    [Column("quantity")]
    public int? Quantity { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Stocks")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SaleDetailId")]
    [InverseProperty("Stocks")]
    public virtual SalesDetail? SaleDetail { get; set; }

    [ForeignKey("SiteId")]
    [InverseProperty("Stocks")]
    public virtual Site Site { get; set; } = null!;

    [ForeignKey("SizeId")]
    [InverseProperty("Stocks")]
    public virtual Size? Size { get; set; }

    [ForeignKey("StockEntryId")]
    [InverseProperty("Stocks")]
    public virtual StockEntry? StockEntry { get; set; }

    [ForeignKey("StockId")]
    [InverseProperty("Stocks")]
    public virtual ICollection<Embroidery> Embroideries { get; set; } = new List<Embroidery>();
}
