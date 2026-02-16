using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("employees")]
public partial class Employee
{
    [Key]
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("appuser_id")]
    [StringLength(450)]
    public string? AppuserId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("first_last_name")]
    [StringLength(100)]
    public string? FirstLastName { get; set; }

    [Column("second_last_name")]
    [StringLength(100)]
    public string? SecondLastName { get; set; }

    [Column("birthdate", TypeName = "timestamp without time zone")]
    public DateTime? Birthdate { get; set; }

    [Column("gender")]
    [StringLength(10)]
    public string? Gender { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime? CreateDate { get; set; }

    [Column("start_date", TypeName = "timestamp without time zone")]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = "timestamp without time zone")]
    public DateTime? EndDate { get; set; }

    [Column("photo")]
    public byte[]? Photo { get; set; }

    [Column("address")]
    [StringLength(150)]
    public string? Address { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("cellphone")]
    [StringLength(10)]
    public string? Cellphone { get; set; }

    [Column("whatsapp")]
    public bool? Whatsapp { get; set; }

    [Column("phone")]
    [StringLength(10)]
    public string? Phone { get; set; }

    [Column("facebook")]
    [StringLength(300)]
    public string? Facebook { get; set; }

    [Column("facebook_profile_image")]
    public byte[]? FacebookProfileImage { get; set; }

    [Column("health_info")]
    [StringLength(10)]
    public string? HealthInfo { get; set; }

    [Column("marital_status")]
    [StringLength(20)]
    public string? MaritalStatus { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [Column("comments")]
    [StringLength(500)]
    public string? Comments { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<AccountPayment> AccountPayments { get; set; } = new List<AccountPayment>();

    [InverseProperty("Employee")]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    [ForeignKey("AppuserId")]
    [InverseProperty("Employees")]
    public virtual AspNetUser? Appuser { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [InverseProperty("Employee")]
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    [InverseProperty("Employee")]
    public virtual ICollection<EmployeeSalary> EmployeeSalaries { get; set; } = new List<EmployeeSalary>();

    [InverseProperty("Employee")]
    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();

    [InverseProperty("Employee")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [InverseProperty("Employee")]
    public virtual ICollection<ProviderAccountPayment> ProviderAccountPayments { get; set; } = new List<ProviderAccountPayment>();

    [InverseProperty("Employee")]
    public virtual ICollection<ProviderAccount> ProviderAccounts { get; set; } = new List<ProviderAccount>();

    [InverseProperty("Employee")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
