using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("providers")]
[Index("LegalName", Name = "ix_providers_legal_name")]
[Index("ProviderStatusId", Name = "ix_providers_status_id")]
public partial class Provider
{
    [Key]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("provider_status_id")]
    public int ProviderStatusId { get; set; }

    [Column("legal_name")]
    [StringLength(200)]
    public string LegalName { get; set; } = null!;

    [Column("trade_name")]
    [StringLength(200)]
    public string? TradeName { get; set; }

    [Column("tax_id")]
    [StringLength(50)]
    public string? TaxId { get; set; }

    [Column("website")]
    [StringLength(250)]
    public string? Website { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Provider")]
    public virtual ICollection<ProductProvider> ProductProviders { get; set; } = new List<ProductProvider>();

    [InverseProperty("Provider")]
    public virtual ICollection<ProductVisualDefinitionProvider> ProductVisualDefinitionProviders { get; set; } = new List<ProductVisualDefinitionProvider>();

    [InverseProperty("Provider")]
    public virtual ProviderAddress? ProviderAddress { get; set; }

    [InverseProperty("Provider")]
    public virtual ProviderBankAccount? ProviderBankAccount { get; set; }

    [InverseProperty("Provider")]
    public virtual ProviderContact? ProviderContact { get; set; }

    [InverseProperty("Provider")]
    public virtual ICollection<ProviderPayable> ProviderPayables { get; set; } = new List<ProviderPayable>();

    [ForeignKey("ProviderStatusId")]
    [InverseProperty("Providers")]
    public virtual ProviderStatus ProviderStatus { get; set; } = null!;

    [InverseProperty("Provider")]
    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    [InverseProperty("Provider")]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    [InverseProperty("Provider")]
    public virtual ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();
}
