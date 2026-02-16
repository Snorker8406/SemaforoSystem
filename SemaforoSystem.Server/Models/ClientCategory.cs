using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("client_categories")]
public partial class ClientCategory
{
    [Key]
    [Column("client_category_id")]
    public int ClientCategoryId { get; set; }

    [Column("category_name")]
    [StringLength(50)]
    public string CategoryName { get; set; } = null!;

    [Column("description")]
    [StringLength(200)]
    public string? Description { get; set; }

    [InverseProperty("ClientCategory")]
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
