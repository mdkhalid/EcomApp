using System.ComponentModel.DataAnnotations;
using EcomApi.Models;

namespace EcomApi.DTOs;

public class ReturnRequestDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateReturnRequestDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class UpdateReturnStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AdminNote { get; set; }
}
