namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// AVA-side prompt construction settings for a model.
    /// These are UI prompt-policy settings, not provider API definitions.
    /// Future RM layer may override values at runtime.
    /// </summary>
    public class ModelContextSettings
    {
        public string          ModelId                  { get; set; } = string.Empty;
        public int             ContextWindow            { get; set; } = 8192;
        public int             DefaultOutputReserve     { get; set; } = 1024;
        public HistoryPolicyType HistoryPolicy          { get; set; } = HistoryPolicyType.RecentMessages;
        public bool            UseFullHistoryPayload    { get; set; } = false;
        public bool            IncludeConversationHistory { get; set; } = true;
        public bool            IncludeToolCalls         { get; set; } = false;
        public bool            IncludeToolMetadata      { get; set; } = false;
        public bool            IncludeMetadata          { get; set; } = false;
        public bool            SupportsInternalMemory   { get; set; } = false;
        public int?            MaxHistoryMessages       { get; set; } = null;
        public bool            AllowManualHistorySelection { get; set; } = true;
    }
}
