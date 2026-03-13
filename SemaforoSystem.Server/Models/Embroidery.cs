using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("embroideries")]
public partial class Embroidery
{
    [Key]
    [Column("embroidery_id")]
    public int EmbroideryId { get; set; }

    [Column("school_id")]
    public int? SchoolId { get; set; }

    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [Column("create_date")]
    public DateTime? CreateDate { get; set; }

    [Column("emb_file")]
    public byte[]? EmbFile { get; set; }

    [Column("dst_file")]
    public byte[]? DstFile { get; set; }

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("stiches")]
    [StringLength(50)]
    public string? Stiches { get; set; }

    [Column("color_secuence")]
    public string? ColorSecuence { get; set; }

    [Column("price")]
    [Precision(19, 4)]
    public decimal? Price { get; set; }

    [Column("image")]
    public byte[]? Image { get; set; }

    [Column("image_design")]
    public byte[]? ImageDesign { get; set; }

    [InverseProperty("Embroidery")]
    public virtual ICollection<ProductComboDetail> ProductComboDetails { get; set; } = new List<ProductComboDetail>();

    [InverseProperty("Embroidery")]
    public virtual ICollection<ProductVisualDefinitionEmbroidery> ProductVisualDefinitionEmbroideries { get; set; } = new List<ProductVisualDefinitionEmbroidery>();

    [ForeignKey("SchoolId")]
    [InverseProperty("Embroideries")]
    public virtual School? School { get; set; }

    [ForeignKey("EmbroideryId")]
    [InverseProperty("Embroideries")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
