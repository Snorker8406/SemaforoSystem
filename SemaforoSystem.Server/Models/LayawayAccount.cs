using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("layaway_accounts")]
public partial class LayawayAccount
{
    [Key]
    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("deposit_amount")]
    [Precision(14, 2)]
    public decimal DepositAmount { get; set; }

    [Column("expected_arrival_date")]
    public DateOnly? ExpectedArrivalDate { get; set; }

    [Column("ready_date")]
    public DateOnly? ReadyDate { get; set; }

    [Column("delivery_date")]
    public DateOnly? DeliveryDate { get; set; }

    [Column("is_ready")]
    public bool IsReady { get; set; }

    [Column("expiration_date")]
    public DateOnly? ExpirationDate { get; set; }

    [Column("cancellation_policy_notes")]
    [StringLength(250)]
    public string? CancellationPolicyNotes { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("LayawayAccount")]
    public virtual Account Account { get; set; } = null!;
}
