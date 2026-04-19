using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("inventory_transactions")]
public partial class InventoryTransaction
{
    [Key]
    [Column("inventory_transaction_id")]
    public long InventoryTransactionId { get; set; }

    [Column("transaction_type")]
    [StringLength(30)]
    public string TransactionType { get; set; } = null!;

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Column("reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("user_id")]
    [StringLength(200)]
    public string? UserId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("InventoryTransaction")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();
}
