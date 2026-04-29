using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sale_statuses")]
[Index("Code", Name = "uq_sale_statuses_code", IsUnique = true)]
public partial class SaleStatus
{
    [Key]
    [Column("sale_status_id")]
    public int SaleStatusId { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [InverseProperty("SaleStatus")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
