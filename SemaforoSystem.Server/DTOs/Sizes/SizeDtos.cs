namespace SemaforoSystem.Server.DTOs.Sizes;

// ─────────────────────────── Size System (header) ───────────────────────────

public class SizeSystemResponse
{
    public int SizeSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public int SizeCount { get; set; }
    public List<SizeResponse> Sizes { get; set; } = [];
}

public class SizeSystemSummary
{
    public int SizeSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateSizeSystemRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string? Description { get; set; }

    public int? SortOrder { get; set; }
}

public class UpdateSizeSystemRequest : CreateSizeSystemRequest;

// ─────────────────────────── Size (detail) ──────────────────────────────────

public class SizeResponse
{
    public int SizeId { get; set; }
    public string SizeValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SizeSystemId { get; set; }
    public string? SizeSystemName { get; set; }
}

public class CreateSizeRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 1)]
    public string SizeValue { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(250)]
    public string? Description { get; set; }
}

public class UpdateSizeRequest : CreateSizeRequest;

// ─────────────────────────── Query params ───────────────────────────────────

public class SizeSystemQueryParameters : Common.QueryParameters;

public class SizeQueryParameters : Common.QueryParameters
{
    /// <summary>Filter sizes by their parent SizeSystem ID.</summary>
    public int? SizeSystemId { get; set; }
}
