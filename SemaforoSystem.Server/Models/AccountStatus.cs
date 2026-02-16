using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_status")]
public partial class AccountStatus
{
    [Key]
    [Column("account_status_id")]
    public int AccountStatusId { get; set; }

    [Column("name")]
    [StringLength(20)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [InverseProperty("AccountStatus")]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
