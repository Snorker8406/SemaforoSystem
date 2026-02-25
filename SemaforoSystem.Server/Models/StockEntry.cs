using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("stock_entries")]
public partial class StockEntry
{
    [Key]
    [Column("stock_entry_id")]
    public int StockEntryId { get; set; }

    [Column("entry_date")]
    public DateTime EntryDate { get; set; }

    [Column("user_id", TypeName = "character varying")]
    public string UserId { get; set; } = null!;

    [Column("comments", TypeName = "character varying")]
    public string? Comments { get; set; }

    [InverseProperty("StockEntry")]
    public virtual ICollection<StockExpense> StockExpenses { get; set; } = new List<StockExpense>();

    [InverseProperty("StockEntry")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
