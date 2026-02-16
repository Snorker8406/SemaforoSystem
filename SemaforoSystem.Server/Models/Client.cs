using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Table("clients")]
public partial class Client
{
    [Key]
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("client_status_id")]
    public int ClientStatusId { get; set; }

    [Column("client_category_id")]
    public int ClientCategoryId { get; set; }

    [Column("create_date", TypeName = "timestamp without time zone")]
    public DateTime? CreateDate { get; set; }

    [Column("last_modify", TypeName = "timestamp without time zone")]
    public DateTime LastModify { get; set; }

    [Column("last_modified_by")]
    public int LastModifiedBy { get; set; }

    [Column("name")]
    [StringLength(30)]
    public string Name { get; set; } = null!;

    [Column("last_name")]
    [StringLength(30)]
    public string LastName { get; set; } = null!;

    [Column("last_name_mother")]
    [StringLength(30)]
    public string? LastNameMother { get; set; }

    [Column("gender")]
    [StringLength(1)]
    public string? Gender { get; set; }

    [Column("account_days_limit")]
    public int? AccountDaysLimit { get; set; }

    [Column("account_amount_limit")]
    [Precision(19, 4)]
    public decimal? AccountAmountLimit { get; set; }

    [Column("address")]
    [StringLength(300)]
    public string Address { get; set; } = null!;

    [Column("cellphone")]
    [StringLength(20)]
    public string Cellphone { get; set; } = null!;

    [Column("whatsapp")]
    public bool Whatsapp { get; set; }

    [Column("facebook")]
    [StringLength(200)]
    public string? Facebook { get; set; }

    [Column("facebook_name")]
    [StringLength(100)]
    public string? FacebookName { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("profile_image")]
    public byte[]? ProfileImage { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [InverseProperty("Client")]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    [ForeignKey("ClientCategoryId")]
    [InverseProperty("Clients")]
    public virtual ClientCategory ClientCategory { get; set; } = null!;

    [ForeignKey("ClientStatusId")]
    [InverseProperty("Clients")]
    public virtual ClientStatus ClientStatus { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("Clients")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("Client")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [InverseProperty("Client")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
