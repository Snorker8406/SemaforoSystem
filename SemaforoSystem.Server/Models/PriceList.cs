using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("price_lists")]
[Index("Name", Name = "price_lists_name_unique", IsUnique = true)]
public partial class PriceList
{
    [Key]
    [Column("price_list_id")]
    public int PriceListId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = null!;

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("PriceList")]
    public virtual ICollection<PriceItemDefinitionEntry> PriceItemDefinitionEntries { get; set; } = new List<PriceItemDefinitionEntry>();

    [InverseProperty("PriceList")]
    public virtual ICollection<PriceProductEntry> PriceProductEntries { get; set; } = new List<PriceProductEntry>();

    [InverseProperty("PriceList")]
    public virtual ICollection<PriceVisualDefinitionEntry> PriceVisualDefinitionEntries { get; set; } = new List<PriceVisualDefinitionEntry>();
}
