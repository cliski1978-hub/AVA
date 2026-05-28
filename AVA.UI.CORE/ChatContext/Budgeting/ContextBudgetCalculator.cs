using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Budgeting
{
    /// <summary>
    /// Calculates PromptBudgetState from explicit token counts.
    /// All inputs are clamped — no throws on bad values.
    /// </summary>
    public class ContextBudgetCalculator : IContextBudgetCalculator
    {
        public PromptBudgetState CalculateBudget(
            string modelId,
            int contextWindow,
            int outputReserve,
            int usedTokens)
        {
            var safeWindow  = Math.Max(0, contextWindow);
            var safeReserve = Math.Max(0, outputReserve);
            var safeUsed    = Math.Max(0, usedTokens);

            var reservedTotal = safeUsed + safeReserve;
            var remaining     = safeWindow - reservedTotal;

            double usagePercent;
            if (safeWindow > 0)
                usagePercent = (double)reservedTotal / safeWindow * 100.0;
            else
                usagePercent = reservedTotal > 0 ? 100.0 : 0.0;

            return new PromptBudgetState
            {
                ModelId         = modelId ?? string.Empty,
                ContextWindow   = safeWindow,
                OutputReserve   = safeReserve,
                UsedTokens      = safeUsed,
                RemainingTokens = remaining,
                UsagePercent    = usagePercent,
                IsOverBudget    = remaining < 0
            };
        }
    }
}
