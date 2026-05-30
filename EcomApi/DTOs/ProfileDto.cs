using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class UpdateProfileDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }
}

public class AddressDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class CreateAddressDto
{
    [Required]
    [StringLength(50)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
}

public class UpdateAddressDto
{
    [StringLength(50)]
    public string? Label { get; set; }

    [StringLength(500)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    public bool? IsDefault { get; set; }
}
