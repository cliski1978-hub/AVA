using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    public interface IContextBudgetCalculator
    {
        PromptBudgetState CalculateBudget(
            string modelId,
            int contextWindow,
            int outputReserve,
            int usedTokens);
    }
}
