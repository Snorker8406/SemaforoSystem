using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_account")]
public partial class ProviderAccount
{
    [Key]
    [Column("provider_account_id")]
    public int ProviderAccountId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("provider_id")]
    public int? ProviderId { get; set; }

    [Column("name")]
    [StringLength(150)]
    public string? Name { get; set; }

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("opening_date", TypeName = "timestamp without time zone")]
    public DateTime OpeningDate { get; set; }

    [Column("settlement_date", TypeName = "timestamp without time zone")]
    public DateTime? SettlementDate { get; set; }

    [Column("balance")]
    [Precision(19, 4)]
    public decimal Balance { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("ProviderAccounts")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("ProviderAccount")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [ForeignKey("ProviderId")]
    [InverseProperty("ProviderAccounts")]
    public virtual Provider? Provider { get; set; }

    [InverseProperty("ProviderAccount")]
    public virtual ICollection<ProviderAccountPayment> ProviderAccountPayments { get; set; } = new List<ProviderAccountPayment>();
}
