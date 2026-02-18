using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.SchoolLevels;

/// <summary>
/// Request body for updating an existing school level.
/// </summary>
public class UpdateSchoolLevelRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [StringLength(250)]
    public string? Description { get; set; }
}
