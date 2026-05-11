using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("attachments")]
public partial class Attachment
{
    [Key]
    [Column("attachment_id")]
    public long AttachmentId { get; set; }

    [Column("file_name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Column("content_type")]
    [StringLength(150)]
    public string ContentType { get; set; } = null!;

    [Column("file_size_bytes")]
    public int? FileSizeBytes { get; set; }

    [Column("sha256")]
    [StringLength(64)]
    public string? Sha256 { get; set; }

    [Column("file_bytes")]
    public byte[] FileBytes { get; set; } = null!;

    [Column("notes")]
    [StringLength(250)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Attachment")]
    public virtual ICollection<AttachmentLink> AttachmentLinks { get; set; } = new List<AttachmentLink>();
}
