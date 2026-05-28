using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Provides deterministic compression for prompt context items.
    /// </summary>
    public interface IContextCompressionService
    {
        /// <summary>
        /// Compresses eligible older context items while preserving recent, pinned, and forced items.
        /// </summary>
        CompressionResult Compress(
            IReadOnlyList<PromptContextItem> items,
            ModelContextSettings modelSettings,
            PromptBudgetState budgetState);
    }
}
