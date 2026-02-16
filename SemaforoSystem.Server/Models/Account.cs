using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("accounts")]
public partial class Account
{
    [Key]
    [Column("account_id")]
    public int AccountId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("account_type_id")]
    public int AccountTypeId { get; set; }

    [Column("sale_id")]
    public int SaleId { get; set; }

    [Column("account_status_id")]
    public int AccountStatusId { get; set; }

    [Column("opening_date", TypeName = "timestamp without time zone")]
    public DateTime OpeningDate { get; set; }

    [Column("settlement_date", TypeName = "timestamp without time zone")]
    public DateTime? SettlementDate { get; set; }

    [Column("cancellation_date", TypeName = "timestamp without time zone")]
    public DateTime? CancellationDate { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("barcode")]
    [StringLength(50)]
    public string Barcode { get; set; } = null!;

    [Column("balance")]
    [Precision(19, 4)]
    public decimal Balance { get; set; }

    [InverseProperty("Account")]
    public virtual ICollection<AccountPayment> AccountPayments { get; set; } = new List<AccountPayment>();

    [ForeignKey("AccountStatusId")]
    [InverseProperty("Accounts")]
    public virtual AccountStatus AccountStatus { get; set; } = null!;

    [ForeignKey("AccountTypeId")]
    [InverseProperty("Accounts")]
    public virtual AccountType AccountType { get; set; } = null!;

    [ForeignKey("ClientId")]
    [InverseProperty("Accounts")]
    public virtual Client Client { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("Accounts")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("Account")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [ForeignKey("SaleId")]
    [InverseProperty("Accounts")]
    public virtual Sale Sale { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("Accounts")]
    public virtual Site Site { get; set; } = null!;
}
