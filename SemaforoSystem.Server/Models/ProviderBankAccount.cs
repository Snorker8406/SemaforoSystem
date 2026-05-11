using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("provider_bank_accounts")]
[Index("ProviderId", Name = "ix_provider_bank_accounts_provider_id")]
public partial class ProviderBankAccount
{
    [Key]
    [Column("provider_bank_account_id")]
    public long ProviderBankAccountId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("bank_name")]
    [StringLength(120)]
    public string BankName { get; set; } = null!;

    [Column("account_holder")]
    [StringLength(200)]
    public string? AccountHolder { get; set; }

    [Column("account_number")]
    [StringLength(60)]
    public string? AccountNumber { get; set; }

    [Column("clabe")]
    [StringLength(30)]
    public string? Clabe { get; set; }

    [Column("currency_code")]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = null!;

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("ProviderBankAccount")]
    public virtual Provider Provider { get; set; } = null!;
}
