namespace EcomApi.Models;

public enum ReturnStatus
{
    Requested,
    Approved,
    Rejected,
    RefundInitiated,
    Refunded
}

public enum ReturnReason
{
    Defective,
    WrongItem,
    NotAsDescribed,
    SizeIssue,
    ChangedMind,
    Other
}

public class ReturnRequest
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ReturnReason Reason { get; set; }
    public string? Comment { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
