namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Response DTO for a provider address.
/// </summary>
public class ProviderAddressResponse
{
    public long ProviderAddressId { get; set; }
    public int ProviderId { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}
