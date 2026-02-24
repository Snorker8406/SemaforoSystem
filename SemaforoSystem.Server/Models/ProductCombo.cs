using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combos")]
public partial class ProductCombo
{
    [Key]
    [Column("product_combo_id")]
    public int ProductComboId { get; set; }

    [Column("name")]
    [StringLength(250)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("create_date")]
    public DateTime? CreateDate { get; set; }

    [Column("active")]
    public bool? Active { get; set; }

    [InverseProperty("ProductCombo")]
    public virtual ICollection<ProductComboDetail> ProductComboDetails { get; set; } = new List<ProductComboDetail>();

    [InverseProperty("ProductCombo")]
    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    [ForeignKey("ProductComboId")]
    [InverseProperty("ProductCombos")]
    public virtual ICollection<School> Schools { get; set; } = new List<School>();
}
