namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Response DTO for a provider bank account.
/// </summary>
public class ProviderBankAccountResponse
{
    public long ProviderBankAccountId { get; set; }
    public int ProviderId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Clabe { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
