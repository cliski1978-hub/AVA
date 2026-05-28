namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Canonical role identifiers for session chat messages.
    /// </summary>
    public static class SessionChatRole
    {
        public const string System     = "system";
        public const string User       = "user";
        public const string Assistant  = "assistant";
        public const string ToolCall   = "tool_call";
        public const string ToolResult = "tool_result";
        public const string Metadata   = "metadata";
    }
}
