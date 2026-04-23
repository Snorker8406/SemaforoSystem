using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("files")]
public partial class File
{
    [Key]
    [Column("file_id")]
    public int FileId { get; set; }

    [Column("client_id")]
    public int? ClientId { get; set; }

    [Column("employee_id")]
    public int? EmployeeId { get; set; }

    [Column("provider_id")]
    public int? ProviderId { get; set; }

    [Column("school_id")]
    public int? SchoolId { get; set; }

    [Column("account_id")]
    public long? AccountId { get; set; }

    [Column("provider_account_id")]
    public int? ProviderAccountId { get; set; }

    [Column("provider_account_payment_id")]
    public int? ProviderAccountPaymentId { get; set; }

    [Column("archive_id")]
    public int? ArchiveId { get; set; }

    [Column("comments")]
    [StringLength(500)]
    public string? Comments { get; set; }

    [Column("file_name")]
    [StringLength(250)]
    public string FileName { get; set; } = null!;

    [Column("content_type")]
    [StringLength(250)]
    public string ContentType { get; set; } = null!;

    [Column("field_type")]
    [StringLength(50)]
    public string FieldType { get; set; } = null!;

    [Column("size")]
    [StringLength(100)]
    public string Size { get; set; } = null!;

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime CreateDate { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("Files")]
    public virtual Account? Account { get; set; }

    [ForeignKey("ArchiveId")]
    [InverseProperty("Files")]
    public virtual Archive? Archive { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("Files")]
    public virtual Client? Client { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Files")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("Files")]
    public virtual Provider? Provider { get; set; }

    [ForeignKey("ProviderAccountId")]
    [InverseProperty("Files")]
    public virtual ProviderAccount? ProviderAccount { get; set; }

    [ForeignKey("ProviderAccountPaymentId")]
    [InverseProperty("Files")]
    public virtual ProviderAccountPayment? ProviderAccountPayment { get; set; }

    [ForeignKey("SchoolId")]
    [InverseProperty("Files")]
    public virtual School? School { get; set; }
}
