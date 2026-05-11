using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_receipt_statuses")]
[Index("Code", Name = "uq_purchase_receipt_statuses_code", IsUnique = true)]
public partial class PurchaseReceiptStatus
{
    [Key]
    [Column("purchase_receipt_status_id")]
    public int PurchaseReceiptStatusId { get; set; }

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

    [InverseProperty("PurchaseReceiptStatus")]
    public virtual ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();
}
