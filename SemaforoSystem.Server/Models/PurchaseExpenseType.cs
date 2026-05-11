using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("purchase_expense_types")]
[Index("Code", Name = "uq_purchase_expense_types_code", IsUnique = true)]
public partial class PurchaseExpenseType
{
    [Key]
    [Column("purchase_expense_type_id")]
    public int PurchaseExpenseTypeId { get; set; }

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

    [InverseProperty("PurchaseExpenseType")]
    public virtual ICollection<PurchaseOrderExpense> PurchaseOrderExpenses { get; set; } = new List<PurchaseOrderExpense>();
}
