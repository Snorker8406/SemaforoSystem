using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("school_levels")]
public partial class SchoolLevel
{
    [Key]
    [Column("school_level_id")]
    public int SchoolLevelId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [InverseProperty("SchoolLevel")]
    public virtual ICollection<School> Schools { get; set; } = new List<School>();
}
