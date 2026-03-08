using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

[Keyless]
public partial class VCurrentBasePricesProduct
{
    [Column("price_entry_id")]
    public long? PriceEntryId { get; set; }

    [Column("price_list_id")]
    public int? PriceListId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("price_amount")]
    [Precision(12, 2)]
    public decimal? PriceAmount { get; set; }

    [Column("price_kind")]
    [StringLength(20)]
    public string? PriceKind { get; set; }

    [Column("priority")]
    public int? Priority { get; set; }

    [Column("promo_name")]
    [StringLength(120)]
    public string? PromoName { get; set; }

    [Column("promo_code")]
    [StringLength(60)]
    public string? PromoCode { get; set; }

    [Column("valid_from")]
    public DateTime? ValidFrom { get; set; }

    [Column("valid_to")]
    public DateTime? ValidTo { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(200)]
    public string? CreatedBy { get; set; }

    [Column("reason")]
    [StringLength(200)]
    public string? Reason { get; set; }
}
