using EcomApi.Models;

namespace EcomApi.Repositories;

public interface ISupportRepository
{
    Task<SupportConversation> CreateConversationAsync(SupportConversation conversation);
    Task<SupportConversation?> GetConversationAsync(int id);
    Task<List<SupportConversation>> GetUserConversationsAsync(int userId);
    Task<List<SupportConversation>> GetGuestConversationsAsync(string sessionId);
    Task<SupportMessage> AddMessageAsync(SupportMessage message);
    Task<List<SupportMessage>> GetMessagesAsync(int conversationId);
    Task<List<SupportConversation>> GetAllConversationsAsync(int page, int pageSize, string? status = null);
    Task<(List<SupportConversation> Items, int TotalCount)> GetEscalatedAsync(int page, int pageSize);
    Task<bool> UpdateStatusAsync(int id, ConversationStatus status);
}
