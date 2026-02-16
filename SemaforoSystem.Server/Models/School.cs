using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("schools")]
public partial class School
{
    [Key]
    [Column("school_id")]
    public int SchoolId { get; set; }

    [Column("school_level_id")]
    public int SchoolLevelId { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime? CreateDate { get; set; }

    [Column("name")]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Column("address")]
    [StringLength(150)]
    public string Address { get; set; } = null!;

    [Column("ciudad")]
    [StringLength(100)]
    public string? Ciudad { get; set; }

    [Column("state")]
    [StringLength(30)]
    public string? State { get; set; }

    [Column("phone_number")]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [Column("principal_info")]
    [StringLength(300)]
    public string? PrincipalInfo { get; set; }

    [Column("logo")]
    public byte[]? Logo { get; set; }

    [Column("email")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("photo")]
    public byte[]? Photo { get; set; }

    [InverseProperty("School")]
    public virtual ICollection<Embroidery> Embroideries { get; set; } = new List<Embroidery>();

    [InverseProperty("School")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [ForeignKey("SchoolLevelId")]
    [InverseProperty("Schools")]
    public virtual SchoolLevel SchoolLevel { get; set; } = null!;

    [ForeignKey("SchoolId")]
    [InverseProperty("Schools")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
