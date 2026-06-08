using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class SupportRepository : ISupportRepository
{
    private readonly ApplicationDbContext _context;

    public SupportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupportConversation> CreateConversationAsync(SupportConversation conversation)
    {
        _context.SupportConversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<SupportConversation?> GetConversationAsync(int id)
    {
        return await _context.SupportConversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<SupportConversation>> GetUserConversationsAsync(int userId)
    {
        return await _context.SupportConversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<SupportConversation>> GetGuestConversationsAsync(string sessionId)
    {
        return await _context.SupportConversations
            .Where(c => c.SessionId == sessionId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<SupportMessage> AddMessageAsync(SupportMessage message)
    {
        _context.SupportMessages.Add(message);
        await _context.SaveChangesAsync();

        var conversation = await _context.SupportConversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return message;
    }

    public async Task<List<SupportMessage>> GetMessagesAsync(int conversationId)
    {
        return await _context.SupportMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SupportConversation>> GetAllConversationsAsync(int page, int pageSize, string? status = null)
    {
        var query = _context.SupportConversations
            .Include(c => c.User)
            .Include(c => c.Messages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConversationStatus>(status, true, out var cs))
        {
            query = query.Where(c => c.Status == cs);
        }

        return await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<(List<SupportConversation> Items, int TotalCount)> GetEscalatedAsync(int page, int pageSize)
    {
        var query = _context.SupportConversations
            .Include(c => c.User)
            .Include(c => c.Messages)
            .Where(c => c.Status == ConversationStatus.Escalated);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> UpdateStatusAsync(int id, ConversationStatus status)
    {
        var conversation = await _context.SupportConversations.FindAsync(id);
        if (conversation == null) return false;

        conversation.Status = status;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
