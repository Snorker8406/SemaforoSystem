using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("providers")]
public partial class Provider
{
    [Key]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("name")]
    [StringLength(250)]
    public string Name { get; set; } = null!;

    [Column("address")]
    [StringLength(250)]
    public string? Address { get; set; }

    [Column("phone")]
    [StringLength(250)]
    public string? Phone { get; set; }

    [Column("contact_name")]
    [StringLength(250)]
    public string ContactName { get; set; } = null!;

    [Column("cellphone")]
    [StringLength(250)]
    public string? Cellphone { get; set; }

    [Column("bank_accounts")]
    [StringLength(500)]
    public string? BankAccounts { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime? CreateDate { get; set; }

    [Column("whatsapp")]
    [StringLength(50)]
    public string? Whatsapp { get; set; }

    [Column("website")]
    [StringLength(250)]
    public string? Website { get; set; }

    [Column("image")]
    public byte[]? Image { get; set; }

    [InverseProperty("Provider")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [InverseProperty("Provider")]
    public virtual ICollection<ProviderAccount> ProviderAccounts { get; set; } = new List<ProviderAccount>();
}
