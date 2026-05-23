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
}
