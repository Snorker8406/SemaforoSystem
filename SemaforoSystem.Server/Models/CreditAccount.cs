using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("credit_accounts")]
public partial class CreditAccount
{
    [Key]
    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("credit_days")]
    public int? CreditDays { get; set; }

    [Column("grace_until")]
    public DateOnly? GraceUntil { get; set; }

    [Column("original_due_date")]
    public DateOnly? OriginalDueDate { get; set; }

    [Column("last_extension_date")]
    public DateOnly? LastExtensionDate { get; set; }

    [Column("extended_due_date")]
    public DateOnly? ExtendedDueDate { get; set; }

    [Column("credit_limit_snapshot")]
    [Precision(14, 2)]
    public decimal? CreditLimitSnapshot { get; set; }

    [Column("requires_guarantor")]
    public bool RequiresGuarantor { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("CreditAccount")]
    public virtual Account Account { get; set; } = null!;
}
