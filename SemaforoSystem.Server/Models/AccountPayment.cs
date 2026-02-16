using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_payments")]
public partial class AccountPayment
{
    [Key]
    [Column("account_payment_id")]
    public int AccountPaymentId { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("payment_date", TypeName = "timestamp without time zone")]
    public DateTime PaymentDate { get; set; }

    [Column("notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountPayments")]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("AccountPayments")]
    public virtual Employee Employee { get; set; } = null!;
}
