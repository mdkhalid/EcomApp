using EcomApi.Models;

namespace EcomApi.DTOs;

public class SupportMessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SupportMessageDto> Messages { get; set; } = new();
    public int MessageCount { get; set; }
}

public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class BotResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public bool NeedsEscalation { get; set; }
    public SupportMessageDto Message { get; set; } = null!;
}
