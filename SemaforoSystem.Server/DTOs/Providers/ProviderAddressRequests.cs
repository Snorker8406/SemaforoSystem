using System.ComponentModel.DataAnnotations;

namespace SemaforoSystem.Server.DTOs.Providers;

public class CreateProviderAddressRequest
{
    [Required]
    [StringLength(30)]
    public string AddressType { get; set; } = null!;

    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string AddressLine { get; set; } = null!;

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    public bool IsPrimary { get; set; }
}

public class UpdateProviderAddressRequest : CreateProviderAddressRequest { }
