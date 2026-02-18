namespace SemaforoSystem.Server.DTOs.Schools;

/// <summary>
/// Response DTO representing a school entity.
/// </summary>
public class SchoolResponse
{
    public int SchoolId { get; set; }
    public int SchoolLevelId { get; set; }
    public string SchoolLevelName { get; set; } = string.Empty;
    public DateTime? CreateDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Ciudad { get; set; }
    public string? State { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PrincipalInfo { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public bool HasLogo { get; set; }
    public bool HasPhoto { get; set; }
}
