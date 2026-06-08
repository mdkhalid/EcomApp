namespace EcomApi.Models;

public enum ConversationStatus
{
    Open,
    Resolved,
    Escalated
}

public enum MessageSender
{
    User,
    Bot,
    Admin
}

public class SupportConversation
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? SessionId { get; set; }
    public string? UserEmail { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<SupportMessage> Messages { get; set; } = new();
}

public class SupportMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public SupportConversation Conversation { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public MessageSender Sender { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
