namespace AVA.UI.CORE.ChatContext.Models
{
    public enum PromptContextItemType
    {
        SystemInstruction = 0,
        ChatMessage       = 1,
        ToolCall          = 2,
        ToolResult        = 3,
        Metadata          = 4,
        CurrentPrompt     = 5,
        MemoryInjection   = 6,
        Other             = 99
    }
}
