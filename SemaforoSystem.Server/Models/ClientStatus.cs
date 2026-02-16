using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("client_status")]
public partial class ClientStatus
{
    [Key]
    [Column("client_status_id")]
    public int ClientStatusId { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [InverseProperty("ClientStatus")]
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
