using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Providers;

public class CreateProviderContactRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? Role { get; set; }

    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(30)]
    public string? Cellphone { get; set; }

    public bool Whatsapp { get; set; }

    public bool IsPrimary { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }
}

public class UpdateProviderContactRequest : CreateProviderContactRequest { }
