using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combo_images")]
[Index("ImageRole", Name = "ix_combo_images_role")]
public partial class ProductComboImage
{
    [Key]
    [Column("product_combo_image_id")]
    public long ProductComboImageId { get; set; }

    [Column("image_bytes")]
    public byte[] ImageBytes { get; set; } = null!;

    [Column("content_type")]
    [StringLength(100)]
    public string ContentType { get; set; } = null!;

    [Column("image_role")]
    [StringLength(20)]
    public string ImageRole { get; set; } = null!;

    [Column("file_name")]
    [StringLength(255)]
    public string? FileName { get; set; }

    [Column("file_size_bytes")]
    public int? FileSizeBytes { get; set; }

    [Column("sha256")]
    [StringLength(64)]
    public string? Sha256 { get; set; }

    [Column("width_px")]
    public int? WidthPx { get; set; }

    [Column("height_px")]
    public int? HeightPx { get; set; }

    [Column("alt_text")]
    [StringLength(200)]
    public string? AltText { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("ProductComboImage")]
    public virtual ICollection<ProductComboImageTarget> ProductComboImageTargets { get; set; } = new List<ProductComboImageTarget>();
}
