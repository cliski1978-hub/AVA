namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents one context usage warning or advisory.
    /// </summary>
    public class ContextUsageWarning
    {
        /// <summary>
        /// Gets or sets the stable warning code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display-ready warning message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning severity.
        /// </summary>
        public string Severity { get; set; } = string.Empty;
    }
}
