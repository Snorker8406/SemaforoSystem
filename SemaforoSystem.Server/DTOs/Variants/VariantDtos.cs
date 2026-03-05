namespace SemaforoSystem.Server.DTOs.Variants;

// ─────────────────────────── Variant System (header) ────────────────────────

public class VariantSystemResponse
{
    public int ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VariantCount { get; set; }
    public int ProductCount { get; set; }
    public List<VariantResponse> Variants { get; set; } = [];
}

public class VariantSystemSummary
{
    public int ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateVariantSystemRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(250)]
    public string? Description { get; set; }
}

public class UpdateVariantSystemRequest : CreateVariantSystemRequest;

// ─────────────────────────── Variant (detail) ───────────────────────────────

public class VariantResponse
{
    public int ProductVariantId { get; set; }
    public string VariantValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductVariantSystemId { get; set; }
    public string? ProductVariantSystemName { get; set; }
}

public class CreateVariantRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(150, MinimumLength = 1)]
    public string VariantValue { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(250)]
    public string? Description { get; set; }
}

public class UpdateVariantRequest : CreateVariantRequest;

// ─────────────────────────── Query params ───────────────────────────────────

public class VariantSystemQueryParameters : Common.QueryParameters;

public class VariantQueryParameters : Common.QueryParameters
{
    /// <summary>Filter variants by their parent ProductVariantSystem ID.</summary>
    public int? ProductVariantSystemId { get; set; }
}
