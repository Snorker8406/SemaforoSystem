using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payable_types")]
[Index("Code", Name = "uq_provider_payable_types_code", IsUnique = true)]
public partial class ProviderPayableType
{
    [Key]
    [Column("provider_payable_type_id")]
    public int ProviderPayableTypeId { get; set; }

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

    [InverseProperty("ProviderPayableType")]
    public virtual ICollection<ProviderPayable> ProviderPayables { get; set; } = new List<ProviderPayable>();
}
