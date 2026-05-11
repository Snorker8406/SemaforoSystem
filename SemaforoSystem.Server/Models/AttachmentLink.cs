using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("attachment_links")]
[Index("AttachmentId", Name = "ix_attachment_links_attachment_id")]
[Index("EntityType", "EntityId", Name = "ix_attachment_links_entity")]
public partial class AttachmentLink
{
    [Key]
    [Column("attachment_link_id")]
    public long AttachmentLinkId { get; set; }

    [Column("attachment_id")]
    public long AttachmentId { get; set; }

    [Column("entity_type")]
    [StringLength(50)]
    public string EntityType { get; set; } = null!;

    [Column("entity_id")]
    public long EntityId { get; set; }

    [Column("attachment_role")]
    [StringLength(50)]
    public string AttachmentRole { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AttachmentId")]
    [InverseProperty("AttachmentLinks")]
    public virtual Attachment Attachment { get; set; } = null!;
}
