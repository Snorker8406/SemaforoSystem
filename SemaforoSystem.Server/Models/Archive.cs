using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("archives")]
public partial class Archive
{
    [Key]
    [Column("archive_id")]
    public int ArchiveId { get; set; }

    [Column("data")]
    public byte[] Data { get; set; } = null!;

    [InverseProperty("Archive")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
