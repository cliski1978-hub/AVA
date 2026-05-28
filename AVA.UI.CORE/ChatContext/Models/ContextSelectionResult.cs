namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Result of history and context selection after policy evaluation and token budgeting.
    /// Produced by IHistorySelectionPolicy and consumed by IPromptContextBuilderService.
    /// </summary>
    public class ContextSelectionResult
    {
        public string                  SessionId   { get; set; } = string.Empty;
        public string                  ModelId     { get; set; } = string.Empty;
        public List<PromptContextItem> Items       { get; set; } = new();
        public PromptBudgetState       BudgetState { get; set; } = new();
        public List<string>            Warnings    { get; set; } = new();

        public IEnumerable<PromptContextItem> IncludedItems => Items.Where(i => i.IsIncluded);
        public IEnumerable<PromptContextItem> ExcludedItems => Items.Where(i => !i.IsIncluded);
    }
}
