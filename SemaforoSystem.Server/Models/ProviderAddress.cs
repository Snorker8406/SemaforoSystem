using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_addresses")]
[Index("ProviderId", Name = "ix_provider_addresses_provider_id")]
public partial class ProviderAddress
{
    [Key]
    [Column("provider_address_id")]
    public long ProviderAddressId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("address_type")]
    [StringLength(30)]
    public string AddressType { get; set; } = null!;

    [Column("address_line")]
    [StringLength(300)]
    public string AddressLine { get; set; } = null!;

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [StringLength(100)]
    public string? State { get; set; }

    [Column("postal_code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("ProviderAddress")]
    public virtual Provider Provider { get; set; } = null!;
}
