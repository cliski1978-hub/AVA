using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.ChatContext.Services
{
    /// <summary>
    /// Owns session chat history persistence and retrieval for the ChatContext layer.
    /// The underlying file format is managed by the Vault/storage infrastructure —
    /// callers work exclusively with SessionChatHistory and SessionChatMessage.
    /// </summary>
    public interface ISessionChatHistoryService
    {
        /// <summary>Creates an empty history file for a new session and marks it active.</summary>
        Task<SessionChatHistory> CreateSessionAsync(
            string title,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default);

        /// <summary>Returns the lightweight index (loaded on startup, never the full logs).</summary>
        Task<List<ChatSessionIndexItem>> GetIndexAsync(CancellationToken ct = default);

        /// <summary>
        /// Loads the full chat history for a session. Returns null if not found.
        /// Called only when the user selects a session — not on startup.
        /// </summary>
        Task<SessionChatHistory?> LoadHistoryAsync(
            string sessionId,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default);

        /// <summary>Appends a single message and flushes to disk immediately.</summary>
        Task AddMessageAsync(
            string sessionId,
            SessionChatMessage message,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default);

        /// <summary>Overwrites the full session history on disk (used for in-place edits).</summary>
        Task SaveHistoryAsync(
            SessionChatHistory history,
            CancellationToken ct = default);

        /// <summary>Removes the session file and its index entry.</summary>
        Task DeleteSessionAsync(string sessionId, CancellationToken ct = default);

        /// <summary>Persists the active session ID across app restarts.</summary>
        Task SetActiveSessionAsync(string sessionId, CancellationToken ct = default);

        /// <summary>Returns the last persisted active session ID, or null.</summary>
        Task<string?> GetActiveSessionIdAsync(CancellationToken ct = default);
    }
}
