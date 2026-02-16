using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("employee_salary")]
public partial class EmployeeSalary
{
    [Key]
    [Column("employee_salary_id")]
    public int EmployeeSalaryId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("weekday")]
    public int? Weekday { get; set; }

    [Column("salary_day")]
    [Precision(19, 4)]
    public decimal? SalaryDay { get; set; }

    [Column("salary_hour")]
    [Precision(19, 4)]
    public decimal? SalaryHour { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmployeeSalaries")]
    public virtual Employee Employee { get; set; } = null!;
}
