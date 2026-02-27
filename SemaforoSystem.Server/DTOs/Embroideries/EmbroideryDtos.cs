namespace SemaforoSystem.Server.DTOs.Embroideries;

// ── Responses ────────────────────────────────────────────

public class EmbroideryResponse
{
    public int EmbroideryId { get; set; }
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Stiches { get; set; }
    public string? ColorSecuence { get; set; }
    public decimal? Price { get; set; }
    public DateTime? CreateDate { get; set; }
    public bool HasEmbFile { get; set; }
    public bool HasDstFile { get; set; }
    public bool HasImage { get; set; }
    public string? ImageDesignBase64 { get; set; }
}

public class EmbroideryLookup
{
    public int EmbroideryId { get; set; }
    public string Name { get; set; } = null!;
    public int? SchoolId { get; set; }
}

// ── Query parameters ─────────────────────────────────────

public class EmbroideryQueryParameters : Common.QueryParameters
{
    public int? SchoolId { get; set; }
}

// ── Create / Update ──────────────────────────────────────

public class CreateEmbroideryRequest
{
    public int? SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Stiches { get; set; }
    public string? ColorSecuence { get; set; }
    public decimal? Price { get; set; }
}

public class UpdateEmbroideryRequest
{
    public int? SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Stiches { get; set; }
    public string? ColorSecuence { get; set; }
    public decimal? Price { get; set; }
}
