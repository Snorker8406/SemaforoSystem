namespace SemaforoSystem.Server.DTOs.SupplyProcess;

/// <summary>
/// Escuela con sus productos escolares y la información de escuelas relacionadas por producto.
/// </summary>
public class SchoolWithProductsResponse
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public string SchoolLevelName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Ciudad { get; set; }
    public string? State { get; set; }
    public int ProductCount { get; set; }
    public List<SchoolProductResponse> Products { get; set; } = [];
}
