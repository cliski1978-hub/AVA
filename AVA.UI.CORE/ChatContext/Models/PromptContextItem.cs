namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// A single selectable item in the prompt context preview.
    /// May represent a chat message, tool call, system instruction, current prompt, or future memory injection.
    /// </summary>
    public class PromptContextItem
    {
        public string                      ItemId          { get; set; } = string.Empty;
        public PromptContextItemType       ItemType        { get; set; }
        public string                      SourceId        { get; set; } = string.Empty;
        public string                      SourceLabel     { get; set; } = string.Empty;
        public string                      Content         { get; set; } = string.Empty;
        public int                         EstimatedTokens { get; set; }
        public bool                        IsIncluded      { get; set; } = false;
        public bool                        IsPinned        { get; set; }
        public bool                        IsUserOverride  { get; set; }
        public PromptContextSelectionStatus SelectionStatus { get; set; } = PromptContextSelectionStatus.NotEvaluated;
        public Dictionary<string, string>  Metadata        { get; set; } = new();
    }
}
