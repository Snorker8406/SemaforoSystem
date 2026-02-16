using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sales_types")]
public partial class SalesType
{
    [Key]
    [Column("sale_type_id")]
    public int SaleTypeId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [InverseProperty("SaleType")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
