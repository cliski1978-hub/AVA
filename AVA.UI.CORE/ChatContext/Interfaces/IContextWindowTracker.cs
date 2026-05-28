using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    public interface IContextWindowTracker
    {
        PromptBudgetState CalculateFromItems(
            string modelId,
            int contextWindow,
            int outputReserve,
            IEnumerable<PromptContextItem> items);
    }
}
