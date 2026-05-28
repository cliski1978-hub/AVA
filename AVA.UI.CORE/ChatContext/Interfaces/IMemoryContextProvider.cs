using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Provides a bridge for external Memory or RM systems to supply prompt-relevant memory items.
    /// </summary>
    public interface IMemoryContextProvider
    {
        /// <summary>
        /// Retrieves memory context for a prompt build.
        /// </summary>
        Task<MemoryRetrievalResult> RetrieveAsync(
            string sessionId,
            string modelId,
            string queryText,
            ModelContextSettings modelSettings,
            CancellationToken cancellationToken = default);
    }
}
