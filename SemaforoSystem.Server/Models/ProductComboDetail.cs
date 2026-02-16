using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combo_details")]
public partial class ProductComboDetail
{
    [Key]
    [Column("product_combo_detail_id")]
    public int ProductComboDetailId { get; set; }

    [Column("product_combo_id")]
    public int ProductComboId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("embroidery_id")]
    public int? EmbroideryId { get; set; }

    [ForeignKey("EmbroideryId")]
    [InverseProperty("ProductComboDetails")]
    public virtual Embroidery? Embroidery { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductComboDetails")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductComboId")]
    [InverseProperty("ProductComboDetails")]
    public virtual ProductCombo ProductCombo { get; set; } = null!;
}
