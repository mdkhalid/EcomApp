namespace EcomApi.DTOs;

public class ReturnPolicyDto
{
    public int ReturnWindowDays { get; set; }
    public bool IsActive { get; set; }
    public string PolicyText { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class UpdateReturnPolicyDto
{
    public int ReturnWindowDays { get; set; } = 7;
    public bool IsActive { get; set; } = true;
    public string PolicyText { get; set; } = string.Empty;
}
