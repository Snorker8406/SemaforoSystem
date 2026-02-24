using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("product_combos")]
[Index("SchoolId", Name = "fki_product_combos_school_id_fkey")]
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

    [Column("school_id")]
    public int? SchoolId { get; set; }

    [InverseProperty("ProductCombo")]
    public virtual ICollection<ProductComboDetail> ProductComboDetails { get; set; } = new List<ProductComboDetail>();

    [InverseProperty("ProductCombo")]
    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    [ForeignKey("SchoolId")]
    [InverseProperty("ProductCombos")]
    public virtual School? School { get; set; }
}
