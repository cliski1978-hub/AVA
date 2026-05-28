namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Calculated context-window usage metrics for an active prompt package.
    /// </summary>
    public class ContextUsageMetrics
    {
        /// <summary>
        /// Gets or sets the active model identifier.
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model context window.
        /// </summary>
        public int ContextWindow { get; set; }

        /// <summary>
        /// Gets or sets the included prompt context tokens.
        /// </summary>
        public int UsedTokens { get; set; }

        /// <summary>
        /// Gets or sets the reserved output token budget.
        /// </summary>
        public int ReservedOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the remaining context tokens after included context and output reserve.
        /// </summary>
        public int RemainingTokens { get; set; }

        /// <summary>
        /// Gets or sets the total usage percentage.
        /// </summary>
        public double UsagePercent { get; set; }

        /// <summary>
        /// Gets or sets whether the prompt exceeds the model context window.
        /// </summary>
        public bool IsOverBudget { get; set; }

        /// <summary>
        /// Gets or sets whether the prompt is near the context limit.
        /// </summary>
        public bool IsNearLimit { get; set; }

        /// <summary>
        /// Gets or sets the estimated remaining response capacity.
        /// </summary>
        public int EstimatedResponseCapacity { get; set; }

        /// <summary>
        /// Gets or sets the included context item count.
        /// </summary>
        public int IncludedItemCount { get; set; }

        /// <summary>
        /// Gets or sets the excluded context item count.
        /// </summary>
        public int ExcludedItemCount { get; set; }

        /// <summary>
        /// Gets or sets the included token usage by context category.
        /// </summary>
        public Dictionary<string, int> CategoryTokenBreakdown { get; set; } = new();

        /// <summary>
        /// Gets or sets calculated warnings and advisories.
        /// </summary>
        public List<ContextUsageWarning> Warnings { get; set; } = new();
    }
}
