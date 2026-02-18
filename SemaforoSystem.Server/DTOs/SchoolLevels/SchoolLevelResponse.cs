namespace SemaforoSystem.Server.DTOs.SchoolLevels;

/// <summary>
/// Response DTO representing a school level entity.
/// </summary>
public class SchoolLevelResponse
{
    public int SchoolLevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SchoolCount { get; set; }
}
