using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("sites")]
public partial class Site
{
    [Key]
    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("logo")]
    public byte[]? Logo { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string? Type { get; set; }

    [Column("address")]
    [StringLength(150)]
    public string Address { get; set; } = null!;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("image")]
    public byte[]? Image { get; set; }

    [Column("location")]
    [StringLength(150)]
    public string? Location { get; set; }

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("color")]
    [StringLength(20)]
    public string Color { get; set; } = null!;

    [InverseProperty("Site")]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    [InverseProperty("Site")]
    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    [InverseProperty("Site")]
    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    [InverseProperty("CurrentSite")]
    public virtual ICollection<InventorySerialItem> InventorySerialItems { get; set; } = new List<InventorySerialItem>();

    [InverseProperty("Site")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLineSites { get; set; } = new List<InventoryTransactionLine>();

    [InverseProperty("SourceSite")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLineSourceSites { get; set; } = new List<InventoryTransactionLine>();

    [InverseProperty("TargetSite")]
    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLineTargetSites { get; set; } = new List<InventoryTransactionLine>();

    [InverseProperty("Site")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    [InverseProperty("Site")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
