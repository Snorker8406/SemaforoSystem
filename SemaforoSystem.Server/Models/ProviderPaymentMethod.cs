using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payment_methods")]
[Index("Code", Name = "uq_provider_payment_methods_code", IsUnique = true)]
public partial class ProviderPaymentMethod
{
    [Key]
    [Column("provider_payment_method_id")]
    public int ProviderPaymentMethodId { get; set; }

    [Column("code")]
    [StringLength(30)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(80)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [InverseProperty("ProviderPaymentMethod")]
    public virtual ICollection<ProviderPayableTransaction> ProviderPayableTransactions { get; set; } = new List<ProviderPayableTransaction>();
}
