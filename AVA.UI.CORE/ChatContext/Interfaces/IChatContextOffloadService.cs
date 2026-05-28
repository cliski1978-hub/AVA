using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Offloads older chat context from active memory to searchable flat files.
    /// Offloaded messages are excluded from prompt context but remain retrievable.
    /// Sprint 3.5: file-based offload only. Future Memory module will index these.
    /// </summary>
    public interface IChatContextOffloadService
    {
        /// <summary>Returns the output file path of the offloaded context.</summary>
        Task<string> OffloadAsync(
            string sessionId,
            string? vaultId,
            string? projectId,
            IEnumerable<SessionChatMessage> messages,
            CancellationToken cancellationToken = default);
    }
}
