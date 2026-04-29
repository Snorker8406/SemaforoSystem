using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("payment_methods")]
[Index("Code", Name = "uq_payment_methods_code", IsUnique = true)]
public partial class PaymentMethod
{
    [Key]
    [Column("payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<SalePayment> SalePayments { get; set; } = new List<SalePayment>();
}
