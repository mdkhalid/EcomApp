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

    public async Task<SupportConversation> CreateConversationAsync(SupportConversation conversation, CancellationToken cancellationToken = default)
    {
        _context.SupportConversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<SupportConversation?> GetConversationAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SupportConversations.AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<SupportConversation>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportConversations.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SupportConversation>> GetGuestConversationsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportConversations.AsNoTracking()
            .Where(c => c.SessionId == sessionId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportMessage> AddMessageAsync(SupportMessage message, CancellationToken cancellationToken = default)
    {
        _context.SupportMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var conversation = await _context.SupportConversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return message;
    }

    public async Task<List<SupportMessage>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SupportConversation>> GetAllConversationsAsync(int page, int pageSize, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SupportConversations.AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<SupportConversation> Items, int TotalCount)> GetEscalatedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SupportConversations.AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Messages)
            .Where(c => c.Status == ConversationStatus.Escalated);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> UpdateStatusAsync(int id, ConversationStatus status, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.SupportConversations.FindAsync(id);
        if (conversation == null) return false;

        conversation.Status = status;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
