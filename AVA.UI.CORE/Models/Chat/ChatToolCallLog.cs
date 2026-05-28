namespace AVA.UI.CORE.Models.Chat
{
    /// <summary>
    /// Full log of a single tool call within a chat message.
    /// ToolCallId must be set via IAvaIdService.NewToolCallId().
    /// </summary>
    public class ChatToolCallLog
    {
        public string ToolCallId { get; set; }
        public string ToolName { get; set; }
        public string ArgumentsJson { get; set; }
        public string ResultJson { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime CompletedUtc { get; set; }
        public bool Succeeded { get; set; }
        public string ErrorMessage { get; set; }

        public ChatToolCallLog()
        {
            ToolCallId    = string.Empty; // Set via IAvaIdService.NewToolCallId()
            ToolName      = string.Empty;
            ArgumentsJson = string.Empty;
            ResultJson    = string.Empty;
            StartedUtc    = DateTime.UtcNow;
            CompletedUtc  = DateTime.UtcNow;
            Succeeded     = false;
            ErrorMessage  = string.Empty;
        }
    }
}
