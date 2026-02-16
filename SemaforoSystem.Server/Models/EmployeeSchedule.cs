using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("employee_schedule")]
public partial class EmployeeSchedule
{
    [Key]
    [Column("employee_schedule_id")]
    public int EmployeeScheduleId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("weekday")]
    public int Weekday { get; set; }

    [Column("start_time")]
    public TimeOnly? StartTime { get; set; }

    [Column("stop_time")]
    public TimeOnly? StopTime { get; set; }

    [Column("break_time")]
    public TimeOnly? BreakTime { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmployeeSchedules")]
    public virtual Employee Employee { get; set; } = null!;
}
