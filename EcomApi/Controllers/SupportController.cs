using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/support")]
public class SupportController : ControllerBase
{
    private readonly ISupportRepository _supportRepository;
    private readonly IBotService _botService;

    public SupportController(ISupportRepository supportRepository, IBotService botService)
    {
        _supportRepository = supportRepository;
        _botService = botService;
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDto>> CreateConversation()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var conversation = new SupportConversation
        {
            UserId = userId,
            SessionId = userId == null ? sessionId : null,
            Status = ConversationStatus.Open
        };

        conversation = await _supportRepository.CreateConversationAsync(conversation);

        var greeting = await _botService.ProcessMessageAsync("hello", conversation.Id, userId);

        var botMessage = await _supportRepository.AddMessageAsync(new SupportMessage
        {
            ConversationId = conversation.Id,
            Content = greeting.reply,
            Sender = MessageSender.Bot
        });

        return Ok(MapConversation(conversation));
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<ActionResult<BotResponseDto>> SendMessage(int id, [FromBody] SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { error = "Message content is required." });

        var conversation = await _supportRepository.GetConversationAsync(id);
        if (conversation == null)
            return NotFound(new { error = "Conversation not found." });

        var userId = GetUserId();
        if (conversation.UserId != null && conversation.UserId != userId)
            return NotFound(new { error = "Conversation not found." });

        if (conversation.Status == ConversationStatus.Resolved)
            return BadRequest(new { error = "This conversation has been resolved." });

        var userMessage = await _supportRepository.AddMessageAsync(new SupportMessage
        {
            ConversationId = id,
            Content = dto.Content,
            Sender = MessageSender.User
        });

        var (reply, needsEscalation) = await _botService.ProcessMessageAsync(dto.Content, id, userId);

        var botMessage = await _supportRepository.AddMessageAsync(new SupportMessage
        {
            ConversationId = id,
            Content = reply,
            Sender = MessageSender.Bot
        });

        if (needsEscalation)
        {
            await _supportRepository.UpdateStatusAsync(id, ConversationStatus.Escalated);
        }

        return Ok(new BotResponseDto
        {
            Reply = reply,
            NeedsEscalation = needsEscalation,
            Message = MapMessage(userMessage)
        });
    }

    [HttpGet("conversations/{id}/messages")]
    public async Task<ActionResult<List<SupportMessageDto>>> GetMessages(int id)
    {
        var conversation = await _supportRepository.GetConversationAsync(id);
        if (conversation == null)
            return NotFound();

        var userId = GetUserId();
        if (conversation.UserId != null && conversation.UserId != userId)
            return NotFound();

        var messages = await _supportRepository.GetMessagesAsync(id);
        return Ok(messages.Select(MapMessage).ToList());
    }

    [HttpGet("conversations/my")]
    public async Task<ActionResult<List<ConversationDto>>> GetMyConversations()
    {
        var userId = GetUserId();

        List<SupportConversation> conversations;
        if (userId != null)
        {
            conversations = await _supportRepository.GetUserConversationsAsync(userId.Value);
        }
        else
        {
            var sessionId = GetSessionId();
            conversations = await _supportRepository.GetGuestConversationsAsync(sessionId ?? "");
        }

        return Ok(conversations.Select(MapConversation).ToList());
    }

    [HttpGet("conversations")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var conversations = await _supportRepository.GetAllConversationsAsync(pageNumber, pageSize, status);
        return Ok(new
        {
            items = conversations.Select(MapConversation),
            totalCount = conversations.Count,
            pageNumber,
            pageSize
        });
    }

    [HttpGet("conversations/escalated")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetEscalated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, totalCount) = await _supportRepository.GetEscalatedAsync(pageNumber, pageSize);
        return Ok(new
        {
            items = items.Select(MapConversation),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    [HttpPut("conversations/{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateSupportStatusDto dto)
    {
        if (!Enum.TryParse<ConversationStatus>(dto.Status, true, out var status))
            return BadRequest(new { error = "Invalid status." });

        var result = await _supportRepository.UpdateStatusAsync(id, status);
        if (!result)
            return NotFound();

        return Ok(new { message = "Status updated." });
    }

    [HttpPost("conversations/{id}/reply")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SupportMessageDto>> AdminReply(int id, [FromBody] SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { error = "Message content is required." });

        var conversation = await _supportRepository.GetConversationAsync(id);
        if (conversation == null)
            return NotFound();

        var message = await _supportRepository.AddMessageAsync(new SupportMessage
        {
            ConversationId = id,
            Content = dto.Content,
            Sender = MessageSender.Admin
        });

        await _supportRepository.UpdateStatusAsync(id, ConversationStatus.Open);

        return Ok(MapMessage(message));
    }

    private static ConversationDto MapConversation(SupportConversation c)
    {
        return new ConversationDto
        {
            Id = c.Id,
            UserId = c.UserId,
            UserEmail = c.User?.Email ?? c.UserEmail,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Messages = c.Messages?.Select(MapMessage).ToList() ?? new(),
            MessageCount = c.Messages?.Count ?? 0
        };
    }

    private static SupportMessageDto MapMessage(SupportMessage m)
    {
        return new SupportMessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Content = m.Content,
            Sender = m.Sender.ToString(),
            CreatedAt = m.CreatedAt
        };
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim) : null;
    }

    private string? GetSessionId()
    {
        HttpContext.Session.TryGetValue("SessionId", out var data);
        if (data != null) return System.Text.Encoding.UTF8.GetString(data);

        var sessionId = Guid.NewGuid().ToString();
        HttpContext.Session.Set("SessionId", System.Text.Encoding.UTF8.GetBytes(sessionId));
        return sessionId;
    }
}

public class UpdateSupportStatusDto
{
    public string Status { get; set; } = string.Empty;
}
