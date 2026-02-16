using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_account_payments")]
public partial class ProviderAccountPayment
{
    [Key]
    [Column("provider_account_payment_id")]
    public int ProviderAccountPaymentId { get; set; }

    [Column("provider_account_id")]
    public int ProviderAccountId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("payment_date", TypeName = "timestamp without time zone")]
    public DateTime PaymentDate { get; set; }

    [Column("notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("ProviderAccountPayments")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("ProviderAccountPayment")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [ForeignKey("ProviderAccountId")]
    [InverseProperty("ProviderAccountPayments")]
    public virtual ProviderAccount ProviderAccount { get; set; } = null!;
}
