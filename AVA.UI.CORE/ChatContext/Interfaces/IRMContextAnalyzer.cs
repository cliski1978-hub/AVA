using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Provides deterministic RM-aware context scoring and prioritization.
    /// </summary>
    public interface IRMContextAnalyzer
    {
        /// <summary>
        /// Scores and prioritizes prompt context items for the current conversational state.
        /// </summary>
        RMContextAnalysisResult Analyze(
            string currentPrompt,
            IReadOnlyList<PromptContextItem> items,
            ModelContextSettings modelSettings,
            PromptBudgetState budgetState);
    }
}
