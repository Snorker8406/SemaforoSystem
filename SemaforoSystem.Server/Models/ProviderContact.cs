using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_contacts")]
[Index("ProviderId", Name = "ix_provider_contacts_provider_id")]
public partial class ProviderContact
{
    [Key]
    [Column("provider_contact_id")]
    public long ProviderContactId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [Column("role")]
    [StringLength(100)]
    public string? Role { get; set; }

    [Column("email")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("cellphone")]
    [StringLength(30)]
    public string? Cellphone { get; set; }

    [Column("whatsapp")]
    public bool Whatsapp { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("ProviderContact")]
    public virtual Provider Provider { get; set; } = null!;
}
