using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Utilities
{
    /// <summary>
    /// Deterministic formatting helpers for prompt context preview and debugging.
    /// Not provider-specific — no API payload formatting.
    /// All methods are null-safe.
    /// </summary>
    public static class PromptFormatter
    {
        public static string FormatRole(ChatMessageRole role) => role switch
        {
            ChatMessageRole.System     => "System",
            ChatMessageRole.User       => "User",
            ChatMessageRole.Assistant  => "Assistant",
            ChatMessageRole.ToolCall   => "Tool Call",
            ChatMessageRole.ToolResult => "Tool Result",
            ChatMessageRole.Metadata   => "Metadata",
            _                          => role.ToString()
        };

        public static string FormatItemType(PromptContextItemType itemType) => itemType switch
        {
            PromptContextItemType.SystemInstruction => "System Instruction",
            PromptContextItemType.ChatMessage       => "Chat Message",
            PromptContextItemType.ToolCall          => "Tool Call",
            PromptContextItemType.ToolResult        => "Tool Result",
            PromptContextItemType.Metadata          => "Metadata",
            PromptContextItemType.CurrentPrompt     => "Current Prompt",
            PromptContextItemType.MemoryInjection   => "Memory Injection",
            PromptContextItemType.Other             => "Other",
            _                                       => itemType.ToString()
        };

        public static string BuildPreviewText(PromptContextItem? item, int maxLength = 240)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Content))
                return string.Empty;

            var text = item.Content.Trim();
            if (text.Length <= maxLength)
                return text;

            return string.Concat(text.AsSpan(0, maxLength - 3), "...");
        }
    }
}
