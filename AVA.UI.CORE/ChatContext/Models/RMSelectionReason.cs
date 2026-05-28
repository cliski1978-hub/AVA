namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents one deterministic RM selection reason.
    /// </summary>
    public class RMSelectionReason
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Weight { get; set; }
    }
}
