using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Budgeting
{
    /// <summary>
    /// Calculates budget state from a collection of PromptContextItem objects.
    /// Only included items count toward used tokens.
    /// </summary>
    public class ContextWindowTracker : IContextWindowTracker
    {
        private readonly IContextBudgetCalculator _calculator;

        public ContextWindowTracker(IContextBudgetCalculator calculator)
        {
            _calculator = calculator;
        }

        public PromptBudgetState CalculateFromItems(
            string modelId,
            int contextWindow,
            int outputReserve,
            IEnumerable<PromptContextItem> items)
        {
            var usedTokens = items?
                .Where(i => i.IsIncluded)
                .Sum(i => Math.Max(0, i.EstimatedTokens)) ?? 0;

            return _calculator.CalculateBudget(modelId, contextWindow, outputReserve, usedTokens);
        }
    }
}
