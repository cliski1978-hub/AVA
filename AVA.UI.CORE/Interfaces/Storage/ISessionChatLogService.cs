using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.Interfaces.Storage
{
    public interface ISessionChatLogService
    {
        Task<ChatSessionLog> CreateSessionAsync(string title);
        Task<List<ChatSessionIndexItem>> GetSessionIndexAsync();
        Task<ChatSessionLog?> GetSessionAsync(string sessionId, string? vaultId = null, string? projectId = null);
        Task SaveSessionAsync(ChatSessionLog session, string? vaultId = null, string? projectId = null);
        Task DeleteSessionAsync(string sessionId);
        Task AddMessageAsync(string sessionId, ChatSessionMessage message, string? vaultId = null, string? projectId = null);
        Task SetActiveSessionIdAsync(string sessionId);
        Task<string?> GetActiveSessionIdAsync();
        Task ClearAllAsync();
    }
}
