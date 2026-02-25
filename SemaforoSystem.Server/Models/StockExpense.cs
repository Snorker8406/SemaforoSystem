using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("stock_expenses")]
[Index("StockEntryId", Name = "fki_stock_expenses_stock_entry_id_fkey")]
public partial class StockExpense
{
    [Key]
    [Column("stock_expense_id")]
    public int StockExpenseId { get; set; }

    [Column("stock_entry_id")]
    public int StockEntryId { get; set; }

    [Column("amount", TypeName = "money")]
    public decimal Amount { get; set; }

    [Column("comments", TypeName = "character varying")]
    public string? Comments { get; set; }

    [ForeignKey("StockEntryId")]
    [InverseProperty("StockExpenses")]
    public virtual StockEntry StockEntry { get; set; } = null!;
}
