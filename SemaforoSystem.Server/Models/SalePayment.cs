using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sale_payments")]
[Index("PaymentDate", Name = "ix_sale_payments_payment_date")]
[Index("PaymentMethodId", Name = "ix_sale_payments_payment_method_id")]
[Index("SaleId", Name = "ix_sale_payments_sale_id")]
public partial class SalePayment
{
    [Key]
    [Column("sale_payment_id")]
    public long SalePaymentId { get; set; }

    [Column("sale_id")]
    public long SaleId { get; set; }

    [Column("payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column("payment_date")]
    public DateTime PaymentDate { get; set; }

    [Column("amount")]
    [Precision(19, 4)]
    public decimal Amount { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("comments")]
    [StringLength(500)]
    public string? Comments { get; set; }

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("SalePayments")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    [ForeignKey("SaleId")]
    [InverseProperty("SalePayments")]
    public virtual Sale Sale { get; set; } = null!;
}
