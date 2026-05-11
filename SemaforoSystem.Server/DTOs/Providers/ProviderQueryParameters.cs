using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Providers;

/// <summary>
/// Query parameters for filtering and paginating providers.
/// </summary>
public class ProviderQueryParameters : QueryParameters
{
    /// <summary>Filter by provider status ID.</summary>
    public int? ProviderStatusId { get; set; }

    /// <summary>Filter by provider status code (e.g. ACTIVE, INACTIVE, BLOCKED).</summary>
    public string? StatusCode { get; set; }

    /// <summary>Filter by tax id (exact match).</summary>
    public string? TaxId { get; set; }
}
