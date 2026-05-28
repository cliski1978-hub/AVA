using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Memory
{
    /// <summary>
    /// Safe default memory context provider used when no Memory implementation is connected.
    /// </summary>
    public class NullMemoryContextProvider : IMemoryContextProvider
    {
        /// <summary>
        /// Returns an empty memory retrieval result without performing retrieval.
        /// </summary>
        public Task<MemoryRetrievalResult> RetrieveAsync(
            string sessionId,
            string modelId,
            string queryText,
            ModelContextSettings modelSettings,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MemoryRetrievalResult
            {
                SessionId = sessionId ?? string.Empty,
                ModelId = modelId ?? string.Empty,
                QueryText = queryText ?? string.Empty,
                RetrievalApplied = false
            });
        }
    }
}
