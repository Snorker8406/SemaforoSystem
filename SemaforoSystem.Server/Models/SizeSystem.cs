using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("size_system")]
[Index("Name", Name = "unique_name_size_sistem", IsUnique = true)]
public partial class SizeSystem
{
    [Key]
    [Column("size_system_id")]
    public int SizeSystemId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(200)]
    public string? Description { get; set; }

    [InverseProperty("SizeSystem")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    [InverseProperty("SizeSystem")]
    public virtual ICollection<Size> Sizes { get; set; } = new List<Size>();
}
