using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payable_transactions")]
[Index("TransactionDate", Name = "ix_provider_payable_transactions_date")]
[Index("ProviderPayableId", Name = "ix_provider_payable_transactions_payable_id")]
public partial class ProviderPayableTransaction
{
    [Key]
    [Column("provider_payable_transaction_id")]
    public long ProviderPayableTransactionId { get; set; }

    [Column("provider_payable_id")]
    public long ProviderPayableId { get; set; }

    [Column("transaction_type")]
    [StringLength(30)]
    public string TransactionType { get; set; } = null!;

    [Column("provider_payment_method_id")]
    public int? ProviderPaymentMethodId { get; set; }

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("created_by_employee_id")]
    public int CreatedByEmployeeId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedByEmployeeId")]
    [InverseProperty("ProviderPayableTransactions")]
    public virtual Employee CreatedByEmployee { get; set; } = null!;

    [ForeignKey("ProviderPayableId")]
    [InverseProperty("ProviderPayableTransactions")]
    public virtual ProviderPayable ProviderPayable { get; set; } = null!;

    [ForeignKey("ProviderPaymentMethodId")]
    [InverseProperty("ProviderPayableTransactions")]
    public virtual ProviderPaymentMethod? ProviderPaymentMethod { get; set; }
}
