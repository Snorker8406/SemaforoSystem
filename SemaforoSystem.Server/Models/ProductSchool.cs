using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[PrimaryKey("ProductId", "SchoolId")]
[Table("product_schools")]
public partial class ProductSchool
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Key]
    [Column("school_id")]
    public int SchoolId { get; set; }

    [Column("inventory_item_definition_id")]
    public int? InventoryItemDefinitionId { get; set; }

    [ForeignKey("InventoryItemDefinitionId")]
    [InverseProperty("ProductSchools")]
    public virtual InventoryItemDefinition? InventoryItemDefinition { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductSchools")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SchoolId")]
    [InverseProperty("ProductSchools")]
    public virtual School School { get; set; } = null!;
}
