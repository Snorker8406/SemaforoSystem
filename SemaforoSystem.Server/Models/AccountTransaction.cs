using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_transactions")]
[Index("AccountId", "TransactionDate", Name = "idx_account_transactions_account_date")]
[Index("AccountId", Name = "idx_account_transactions_account_id")]
[Index("TransactionDate", Name = "idx_account_transactions_transaction_date")]
public partial class AccountTransaction
{
    [Key]
    [Column("account_transaction_id")]
    public long AccountTransactionId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("transaction_type")]
    [StringLength(30)]
    public string TransactionType { get; set; } = null!;

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("payment_method")]
    [StringLength(30)]
    public string? PaymentMethod { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("created_by_employee_id")]
    public int CreatedByEmployeeId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountTransactions")]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey("CreatedByEmployeeId")]
    [InverseProperty("AccountTransactions")]
    public virtual Employee CreatedByEmployee { get; set; } = null!;
}
