using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sales")]
[Index("AccountId", Name = "ix_sales_account_id")]
[Index("ClientId", Name = "ix_sales_client_id")]
[Index("EmployeeId", Name = "ix_sales_employee_id")]
[Index("LegacySaleId", Name = "ix_sales_legacy_sale_id")]
[Index("SaleDate", Name = "ix_sales_sale_date")]
[Index("SaleStatusId", Name = "ix_sales_sale_status_id")]
[Index("SaleTypeId", Name = "ix_sales_sale_type_id")]
[Index("SiteId", Name = "ix_sales_site_id")]
public partial class Sale
{
    [Key]
    [Column("sale_id")]
    public long SaleId { get; set; }

    [Column("sale_type_id")]
    public int SaleTypeId { get; set; }

    [Column("sale_status_id")]
    public int SaleStatusId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("client_id")]
    public int? ClientId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("account_id")]
    public long? AccountId { get; set; }

    [Column("sale_date")]
    public DateTime SaleDate { get; set; }

    [Column("folio")]
    [StringLength(50)]
    public string? Folio { get; set; }

    [Column("external_reference")]
    [StringLength(100)]
    public string? ExternalReference { get; set; }

    [Column("legacy_sale_id")]
    public int? LegacySaleId { get; set; }

    [Column("subtotal")]
    [Precision(19, 4)]
    public decimal Subtotal { get; set; }

    [Column("discount_total")]
    [Precision(19, 4)]
    public decimal DiscountTotal { get; set; }

    [Column("tax_total")]
    [Precision(19, 4)]
    public decimal TaxTotal { get; set; }

    [Column("total")]
    [Precision(19, 4)]
    public decimal Total { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("Sales")]
    public virtual Account? Account { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("Sales")]
    public virtual Client? Client { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Sales")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("Sale")]
    public virtual ICollection<SalePayment> SalePayments { get; set; } = new List<SalePayment>();

    [ForeignKey("SaleStatusId")]
    [InverseProperty("Sales")]
    public virtual SaleStatus SaleStatus { get; set; } = null!;

    [ForeignKey("SaleTypeId")]
    [InverseProperty("Sales")]
    public virtual SalesType SaleType { get; set; } = null!;

    [InverseProperty("Sale")]
    public virtual ICollection<SalesLine> SalesLines { get; set; } = new List<SalesLine>();

    [ForeignKey("SiteId")]
    [InverseProperty("Sales")]
    public virtual Site Site { get; set; } = null!;
}
