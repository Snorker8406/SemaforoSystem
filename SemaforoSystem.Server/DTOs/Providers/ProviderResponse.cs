namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Lightweight response DTO representing a provider master row.
/// </summary>
public class ProviderResponse
{
    public int ProviderId { get; set; }
    public int ProviderStatusId { get; set; }
    public string ProviderStatusCode { get; set; } = string.Empty;
    public string ProviderStatusName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? TaxId { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ContactsCount { get; set; }
    public int AddressesCount { get; set; }
    public int BankAccountsCount { get; set; }
}

/// <summary>
/// Detailed response DTO including nested contacts, addresses and bank accounts.
/// </summary>
public class ProviderDetailResponse : ProviderResponse
{
    public List<ProviderContactResponse> Contacts { get; set; } = [];
    public List<ProviderAddressResponse> Addresses { get; set; } = [];
    public List<ProviderBankAccountResponse> BankAccounts { get; set; } = [];
}
