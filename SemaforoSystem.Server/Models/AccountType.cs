using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("account_types")]
public partial class AccountType
{
    [Key]
    [Column("account_type_id")]
    public int AccountTypeId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(250)]
    public string? Description { get; set; }

    [InverseProperty("AccountType")]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
