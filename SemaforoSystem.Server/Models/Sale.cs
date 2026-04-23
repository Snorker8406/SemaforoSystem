using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sales")]
public partial class Sale
{
    [Key]
    [Column("sale_id")]
    public int SaleId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("client_id")]
    public int? ClientId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("sale_type_id")]
    public int SaleTypeId { get; set; }

    [Column("sale_date", TypeName = "timestamp without time zone")]
    public DateTime? SaleDate { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [Column("client_name")]
    [StringLength(150)]
    public string? ClientName { get; set; }

    [Column("total")]
    [Precision(19, 4)]
    public decimal? Total { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("Sales")]
    public virtual Client? Client { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Sales")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("SaleTypeId")]
    [InverseProperty("Sales")]
    public virtual SalesType SaleType { get; set; } = null!;

    [InverseProperty("Sale")]
    public virtual ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();

    [ForeignKey("SiteId")]
    [InverseProperty("Sales")]
    public virtual Site Site { get; set; } = null!;
}
