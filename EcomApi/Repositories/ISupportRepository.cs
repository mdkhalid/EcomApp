using EcomApi.Models;

namespace EcomApi.Repositories;

public interface ISupportRepository
{
    Task<SupportConversation> CreateConversationAsync(SupportConversation conversation, CancellationToken cancellationToken = default);
    Task<SupportConversation?> GetConversationAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SupportConversation>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<SupportConversation>> GetGuestConversationsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<SupportMessage> AddMessageAsync(SupportMessage message, CancellationToken cancellationToken = default);
    Task<List<SupportMessage>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<List<SupportConversation>> GetAllConversationsAsync(int page, int pageSize, string? status = null, CancellationToken cancellationToken = default);
    Task<(List<SupportConversation> Items, int TotalCount)> GetEscalatedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, ConversationStatus status, CancellationToken cancellationToken = default);
}
