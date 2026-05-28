namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents one categorized section in a prompt debug package.
    /// </summary>
    public class PromptDebugSection
    {
        /// <summary>
        /// Gets or sets the stable section identifier.
        /// </summary>
        public string SectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the representative prompt context item type.
        /// </summary>
        public PromptContextItemType ItemType { get; set; } = PromptContextItemType.Other;

        /// <summary>
        /// Gets or sets the estimated token total for this section.
        /// </summary>
        public int EstimatedTokens { get; set; }

        /// <summary>
        /// Gets or sets whether this section should initially render collapsed.
        /// </summary>
        public bool IsCollapsed { get; set; }

        /// <summary>
        /// Gets or sets the readable section content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt items contained in this section.
        /// </summary>
        public List<PromptContextItem> Items { get; set; } = new();
    }
}
