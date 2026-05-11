using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_payable_statuses")]
[Index("Code", Name = "uq_provider_payable_statuses_code", IsUnique = true)]
public partial class ProviderPayableStatus
{
    [Key]
    [Column("provider_payable_status_id")]
    public int ProviderPayableStatusId { get; set; }

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

    [InverseProperty("ProviderPayableStatus")]
    public virtual ICollection<ProviderPayable> ProviderPayables { get; set; } = new List<ProviderPayable>();
}
