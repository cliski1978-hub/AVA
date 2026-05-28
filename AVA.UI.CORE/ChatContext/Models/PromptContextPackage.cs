namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Final AVA-side prompt context package assembled and ready for dispatch to a model.
    /// This is not the provider-specific request body — it is the AVA assembled context.
    /// Produced by IPromptContextBuilderService.
    /// </summary>
    public class PromptContextPackage
    {
        public string                  SessionId     { get; set; } = string.Empty;
        public string                  ModelId       { get; set; } = string.Empty;
        public string                  CurrentPrompt { get; set; } = string.Empty;
        public List<PromptContextItem> IncludedItems { get; set; } = new();
        public PromptBudgetState       BudgetState   { get; set; } = new();
        public DateTime                CreatedAt     { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata   { get; set; } = new();
    }
}
