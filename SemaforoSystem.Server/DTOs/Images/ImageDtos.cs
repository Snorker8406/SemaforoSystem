using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Images;

// ═══════════════════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Lightweight DTO returned in list endpoints (no image_bytes).
/// </summary>
public class ProductImageDto
{
    public long ProductImageId { get; set; }
    public string ImageRole { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int? FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public string? AltText { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for a target link (which product or item_definition the image is attached to).
/// </summary>
public class ProductImageTargetDto
{
    public long ProductImageTargetId { get; set; }
    public long ProductImageId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public int? InventoryItemDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Image metadata (joined)
    public ProductImageDto? Image { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Request DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Metadata sent alongside the uploaded file (as form fields).
/// </summary>
public class UploadImageMetadata
{
    /// <summary>PRODUCT or ITEM_DEFINITION</summary>
    [Required]
    [StringLength(30)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Required when target_type = PRODUCT</summary>
    public int? ProductId { get; set; }

    /// <summary>Required when target_type = ITEM_DEFINITION</summary>
    public int? InventoryItemDefinitionId { get; set; }

    /// <summary>ORIGINAL, THUMB, DETAIL</summary>
    [StringLength(20)]
    public string ImageRole { get; set; } = "ORIGINAL";

    [StringLength(200)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// For reordering / changing primary flag of a target.
/// </summary>
public class UpdateImageTargetRequest
{
    public int? SortOrder { get; set; }
    public bool? IsPrimary { get; set; }
}
