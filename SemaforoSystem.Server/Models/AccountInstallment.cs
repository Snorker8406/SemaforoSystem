using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_installments")]
[Index("AccountId", "InstallmentNumber", Name = "account_installments_account_number_key", IsUnique = true)]
[Index("AccountId", Name = "idx_account_installments_account_id")]
[Index("DueDate", Name = "idx_account_installments_due_date")]
public partial class AccountInstallment
{
    [Key]
    [Column("account_installment_id")]
    public long AccountInstallmentId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("installment_number")]
    public int InstallmentNumber { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("expected_amount")]
    [Precision(14, 2)]
    public decimal ExpectedAmount { get; set; }

    [Column("paid_amount")]
    [Precision(14, 2)]
    public decimal PaidAmount { get; set; }

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountInstallments")]
    public virtual Account Account { get; set; } = null!;
}
