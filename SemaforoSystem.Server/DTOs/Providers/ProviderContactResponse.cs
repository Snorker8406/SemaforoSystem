namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Response DTO for a provider contact.
/// </summary>
public class ProviderContactResponse
{
    public long ProviderContactId { get; set; }
    public int ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Cellphone { get; set; }
    public bool Whatsapp { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
