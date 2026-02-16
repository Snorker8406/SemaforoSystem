using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Keyless]
[Table("product_providers")]
public partial class ProductProvider
{
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ProviderId")]
    public virtual Provider Provider { get; set; } = null!;
}
