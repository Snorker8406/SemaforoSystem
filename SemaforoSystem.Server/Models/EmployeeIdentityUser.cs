using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("employee_identity_users")]
[Index("EmployeeId", Name = "employee_identity_users_employee_id_key", IsUnique = true)]
[Index("EmployeeId", Name = "idx_employee_identity_users_employee_id")]
public partial class EmployeeIdentityUser
{
    [Key]
    [Column("aspnet_user_id")]
    [StringLength(450)]
    public string AspnetUserId { get; set; } = null!;

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AspnetUserId")]
    [InverseProperty("EmployeeIdentityUser")]
    public virtual AspNetUser AspnetUser { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmployeeIdentityUser")]
    public virtual Employee Employee { get; set; } = null!;
}
