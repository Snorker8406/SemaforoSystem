using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

/// <summary>
/// costos de los productos
/// </summary>
[Table("product_costs")]
public partial class ProductCost
{
    [Key]
    [Column("product_cost_id")]
    public int ProductCostId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("cost", TypeName = "money")]
    public decimal Cost { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductCosts")]
    public virtual Product Product { get; set; } = null!;
}
