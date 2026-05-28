using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Selects which messages from session history to include in the prompt context.
    /// Deterministic rule-based selection only — no semantic ranking.
    /// Future RM layer will replace or extend this with weighted selection.
    /// </summary>
    public interface IHistorySelectionPolicy
    {
        ContextSelectionResult SelectHistory(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings);
    }
}
