using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("accounts")]
[Index("AccountStatusId", Name = "idx_accounts_account_status_id")]
[Index("AccountTypeId", Name = "idx_accounts_account_type_id")]
[Index("ClientId", Name = "idx_accounts_client_id")]
[Index("OpeningDate", Name = "idx_accounts_opening_date")]
[Index("SiteId", Name = "idx_accounts_site_id")]
public partial class Account
{
    [Key]
    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("opened_by_employee_id")]
    public int OpenedByEmployeeId { get; set; }

    [Column("account_type_id")]
    public int AccountTypeId { get; set; }

    [Column("account_status_id")]
    public int AccountStatusId { get; set; }

    [Column("opening_date")]
    public DateTime OpeningDate { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("closed_date")]
    public DateTime? ClosedDate { get; set; }

    [Column("canceled_date")]
    public DateTime? CanceledDate { get; set; }

    [Column("currency_code")]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = null!;

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Account")]
    public virtual ICollection<AccountInstallment> AccountInstallments { get; set; } = new List<AccountInstallment>();

    [InverseProperty("Account")]
    public virtual ICollection<AccountItem> AccountItems { get; set; } = new List<AccountItem>();

    [ForeignKey("AccountStatusId")]
    [InverseProperty("Accounts")]
    public virtual AccountStatus AccountStatus { get; set; } = null!;

    [InverseProperty("Account")]
    public virtual ICollection<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();

    [ForeignKey("AccountTypeId")]
    [InverseProperty("Accounts")]
    public virtual AccountType AccountType { get; set; } = null!;

    [ForeignKey("ClientId")]
    [InverseProperty("Accounts")]
    public virtual Client Client { get; set; } = null!;

    [InverseProperty("Account")]
    public virtual CreditAccount? CreditAccount { get; set; }

    [InverseProperty("Account")]
    public virtual LayawayAccount? LayawayAccount { get; set; }

    [ForeignKey("OpenedByEmployeeId")]
    [InverseProperty("Accounts")]
    public virtual Employee OpenedByEmployee { get; set; } = null!;

    [InverseProperty("Account")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    [ForeignKey("SiteId")]
    [InverseProperty("Accounts")]
    public virtual Site Site { get; set; } = null!;
}
