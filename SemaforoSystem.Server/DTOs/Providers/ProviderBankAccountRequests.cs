using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Providers;

public class CreateProviderBankAccountRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string BankName { get; set; } = null!;

    [StringLength(200)]
    public string? AccountHolder { get; set; }

    [StringLength(60)]
    public string? AccountNumber { get; set; }

    [StringLength(30)]
    public string? Clabe { get; set; }

    [Required]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = null!;

    public bool IsPrimary { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }
}

public class UpdateProviderBankAccountRequest : CreateProviderBankAccountRequest { }
