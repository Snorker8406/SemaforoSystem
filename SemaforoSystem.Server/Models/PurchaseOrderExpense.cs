using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_order_expenses")]
[Index("PurchaseOrderId", Name = "ix_purchase_order_expenses_order_id")]
public partial class PurchaseOrderExpense
{
    [Key]
    [Column("purchase_order_expense_id")]
    public long PurchaseOrderExpenseId { get; set; }

    [Column("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [Column("purchase_expense_type_id")]
    public int PurchaseExpenseTypeId { get; set; }

    [Column("provider_payable_id")]
    public long? ProviderPayableId { get; set; }

    [Column("description")]
    [StringLength(250)]
    public string Description { get; set; } = null!;

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("currency_code")]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = null!;

    [Column("expense_date")]
    public DateTime ExpenseDate { get; set; }

    [Column("paid_to_provider")]
    public bool PaidToProvider { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProviderPayableId")]
    [InverseProperty("PurchaseOrderExpenses")]
    public virtual ProviderPayable? ProviderPayable { get; set; }

    [ForeignKey("PurchaseExpenseTypeId")]
    [InverseProperty("PurchaseOrderExpenses")]
    public virtual PurchaseExpenseType PurchaseExpenseType { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseOrderExpenses")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
