using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sizes")]
public partial class Size
{
    [Key]
    [Column("size_id")]
    public int SizeId { get; set; }

    [Column("size_value")]
    [StringLength(50)]
    public string SizeValue { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("size_system_id")]
    public int? SizeSystemId { get; set; }

    [InverseProperty("Size")]
    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    [InverseProperty("Size")]
    public virtual ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();

    [ForeignKey("SizeSystemId")]
    [InverseProperty("Sizes")]
    public virtual SizeSystem? SizeSystem { get; set; }

    [InverseProperty("Size")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
