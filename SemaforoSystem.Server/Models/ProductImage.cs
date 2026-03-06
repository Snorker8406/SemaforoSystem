using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_images")]
[Index("ImageRole", Name = "ix_product_images_role")]
public partial class ProductImage
{
    [Key]
    [Column("product_image_id")]
    public long ProductImageId { get; set; }

    [Column("image_bytes")]
    public byte[] ImageBytes { get; set; } = null!;

    [Column("content_type")]
    [StringLength(100)]
    public string ContentType { get; set; } = null!;

    [Column("file_name")]
    [StringLength(255)]
    public string? FileName { get; set; }

    [Column("file_size_bytes")]
    public int? FileSizeBytes { get; set; }

    [Column("sha256")]
    [StringLength(64)]
    public string? Sha256 { get; set; }

    [Column("image_role")]
    [StringLength(20)]
    public string ImageRole { get; set; } = null!;

    [Column("width_px")]
    public int? WidthPx { get; set; }

    [Column("height_px")]
    public int? HeightPx { get; set; }

    [Column("storage_key")]
    [StringLength(300)]
    public string? StorageKey { get; set; }

    [Column("url")]
    [StringLength(500)]
    public string? Url { get; set; }

    [Column("alt_text")]
    [StringLength(200)]
    public string? AltText { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("ProductImage")]
    public virtual ICollection<ProductImageTarget> ProductImageTargets { get; set; } = new List<ProductImageTarget>();
}
