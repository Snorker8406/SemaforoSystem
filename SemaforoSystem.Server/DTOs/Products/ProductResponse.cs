namespace SemaforoSystem.Server.DTOs.Products;

/// <summary>
/// Response DTO representing a product entity.
/// </summary>
public class ProductResponse
{
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? Model { get; set; }
    public string? Comments { get; set; }
    public long? SerialCount { get; set; }
    public bool? Serialize { get; set; }
    public DateTime? CreateDate { get; set; }

    // Brand
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }

    // Size system
    public int? SizeSystemId { get; set; }
    public string? SizeSystemName { get; set; }

    // Categories
    public List<CategoryInfo> Categories { get; set; } = [];

    // Related counts & summary data
    public int SchoolCount { get; set; }
    public bool HasPicture { get; set; }
    public int StockTotal { get; set; }
    public decimal? LatestCost { get; set; }
    public decimal? LatestPrice { get; set; }

    // Variant systems
    public List<VariantSystemInfo> VariantSystems { get; set; } = [];
}

public class CategoryInfo
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VariantSystemInfo
{
    public int ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// School info with its school level, returned by the /schools sub-resource.
/// </summary>
public class ProductSchoolInfo
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SchoolLevelId { get; set; }
    public string SchoolLevelName { get; set; } = string.Empty;
}
