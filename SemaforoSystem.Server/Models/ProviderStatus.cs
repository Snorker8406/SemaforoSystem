using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_statuses")]
[Index("Code", Name = "uq_provider_statuses_code", IsUnique = true)]
[Index("Name", Name = "uq_provider_statuses_name", IsUnique = true)]
public partial class ProviderStatus
{
    [Key]
    [Column("provider_status_id")]
    public int ProviderStatusId { get; set; }

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

    [InverseProperty("ProviderStatus")]
    public virtual ICollection<Provider> Providers { get; set; } = new List<Provider>();
}
