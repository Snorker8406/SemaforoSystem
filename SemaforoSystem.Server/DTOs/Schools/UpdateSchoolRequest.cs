using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Schools;

/// <summary>
/// Request body for updating an existing school.
/// </summary>
public class UpdateSchoolRequest
{
    [Required]
    public int SchoolLevelId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Address { get; set; } = null!;

    [StringLength(100)]
    public string? Ciudad { get; set; }

    [StringLength(30)]
    public string? State { get; set; }

    [StringLength(50)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    public string? PrincipalInfo { get; set; }

    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
