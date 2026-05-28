namespace AVA.UI.CORE.ChatContext.Models
{
    public enum ChatMessageRole
    {
        System     = 0,
        User       = 1,
        Assistant  = 2,
        ToolCall   = 3,
        ToolResult = 4,
        Metadata   = 5
    }
}
