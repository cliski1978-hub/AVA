namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Fully inspectable debug representation of an assembled prompt context package.
    /// </summary>
    public class PromptDebugPackage
    {
        /// <summary>
        /// Gets or sets the source session identifier.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target model identifier.
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC generation timestamp.
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the full readable assembled prompt text.
        /// </summary>
        public string FullPromptText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total estimated token count.
        /// </summary>
        public int TotalEstimatedTokens { get; set; }

        /// <summary>
        /// Gets or sets the ordered prompt debug sections.
        /// </summary>
        public List<PromptDebugSection> Sections { get; set; } = new();

        /// <summary>
        /// Gets or sets package metadata preserved for inspection.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
