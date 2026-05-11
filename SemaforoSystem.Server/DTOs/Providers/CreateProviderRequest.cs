using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Request body for creating a provider master row.
/// </summary>
public class CreateProviderRequest
{
    [Required]
    public int ProviderStatusId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string LegalName { get; set; } = null!;

    [StringLength(200)]
    public string? TradeName { get; set; }

    [StringLength(50)]
    public string? TaxId { get; set; }

    [StringLength(250)]
    [Url]
    public string? Website { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
