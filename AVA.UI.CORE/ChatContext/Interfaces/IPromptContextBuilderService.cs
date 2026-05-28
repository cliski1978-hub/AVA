using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Assembles a PromptContextPackage from session history for a specific model.
    /// Deterministic rule-based assembly only — no semantic reasoning.
    /// Future RM layer will call this and may augment the result.
    /// </summary>
    public interface IPromptContextBuilderService
    {
        /// <summary>
        /// Builds prompt context without external memory retrieval.
        /// </summary>
        PromptContextPackage BuildPromptContext(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings);

        /// <summary>
        /// Builds prompt context and allows the configured memory bridge to inject retrieved memory items.
        /// </summary>
        Task<PromptContextPackage> BuildPromptContextAsync(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings,
            CancellationToken cancellationToken = default);
    }
}
