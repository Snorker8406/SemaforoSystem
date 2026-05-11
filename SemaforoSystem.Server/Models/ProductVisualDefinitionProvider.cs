using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductVisualDefinitionId", "ProviderId")]
[Table("product_visual_definition_providers")]
[Index("ProviderId", Name = "ix_pvdp_provider_id")]
public partial class ProductVisualDefinitionProvider
{
    [Key]
    [Column("product_visual_definition_id")]
    public long ProductVisualDefinitionId { get; set; }

    [Key]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("provider_sku")]
    [StringLength(100)]
    public string? ProviderSku { get; set; }

    [Column("provider_product_name")]
    [StringLength(200)]
    public string? ProviderProductName { get; set; }

    [Column("lead_time_days")]
    public int? LeadTimeDays { get; set; }

    [Column("minimum_order_quantity")]
    public int? MinimumOrderQuantity { get; set; }

    [Column("last_cost")]
    [Precision(19, 4)]
    public decimal? LastCost { get; set; }

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductVisualDefinitionId")]
    [InverseProperty("ProductVisualDefinitionProvider")]
    public virtual ProductVisualDefinition ProductVisualDefinition { get; set; } = null!;

    [ForeignKey("ProviderId")]
    [InverseProperty("ProductVisualDefinitionProviders")]
    public virtual Provider Provider { get; set; } = null!;
}
