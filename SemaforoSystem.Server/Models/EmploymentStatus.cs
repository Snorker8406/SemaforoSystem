using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("employment_statuses")]
[Index("Code", Name = "employment_statuses_code_key", IsUnique = true)]
[Index("Name", Name = "employment_statuses_name_key", IsUnique = true)]
public partial class EmploymentStatus
{
    [Key]
    [Column("employment_status_id")]
    public int EmploymentStatusId { get; set; }

    [Column("code")]
    [StringLength(30)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(80)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [InverseProperty("EmploymentStatus")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
