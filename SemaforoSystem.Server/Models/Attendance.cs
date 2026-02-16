using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("attendance")]
public partial class Attendance
{
    [Key]
    [Column("attendance_id")]
    public int AttendanceId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("weekday")]
    public int Weekday { get; set; }

    [Column("start_date", TypeName = "timestamp without time zone")]
    public DateTime? StartDate { get; set; }

    [Column("stop_date", TypeName = "timestamp without time zone")]
    public DateTime? StopDate { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Attendances")]
    public virtual Employee Employee { get; set; } = null!;
}
