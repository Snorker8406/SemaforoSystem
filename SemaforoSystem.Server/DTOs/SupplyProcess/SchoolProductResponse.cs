namespace SemaforoSystem.Server.DTOs.SupplyProcess;

/// <summary>
/// Producto escolar con información de las escuelas a las que está asignado.
/// </summary>
public class SchoolProductResponse
{
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public long? SerialCount { get; set; }
    public string CategoryName { get; set; } = null!;
    public List<string> SchoolNames { get; set; } = [];
    public int SchoolCount { get; set; }

    /// <summary>
    /// <c>true</c> cuando <see cref="SchoolCount"/> es mayor o igual al umbral recibido como parámetro.
    /// </summary>
    public bool SchoolCommonProduct { get; set; }
}
