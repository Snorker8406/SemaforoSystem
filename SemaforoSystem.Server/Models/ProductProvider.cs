using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductId", "ProviderId")]
[Table("product_providers")]
public partial class ProductProvider
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Key]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductProvider")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ProviderId")]
    [InverseProperty("ProductProviders")]
    public virtual Provider Provider { get; set; } = null!;
}
