using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_pictures")]
public partial class ProductPicture
{
    [Key]
    [Column("product_picture_id")]
    public int ProductPictureId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("picture")]
    public byte[]? Picture { get; set; }

    [Column("create_date")]
    public DateOnly CreateDate { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductPictures")]
    public virtual Product Product { get; set; } = null!;

    [InverseProperty("ProductPicture")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
