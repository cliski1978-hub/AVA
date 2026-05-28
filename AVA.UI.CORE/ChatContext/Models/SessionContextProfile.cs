namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Reusable deterministic prompt-context preset for an active session.
    /// </summary>
    public class SessionContextProfile
    {
        /// <summary>
        /// Gets or sets the stable profile identifier.
        /// </summary>
        public string ProfileId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the built-in profile type.
        /// </summary>
        public SessionContextProfileType ProfileType { get; set; } = SessionContextProfileType.Default;

        /// <summary>
        /// Gets or sets the history selection policy.
        /// </summary>
        public HistoryPolicyType HistoryPolicy { get; set; } = HistoryPolicyType.RecentMessages;

        /// <summary>
        /// Gets or sets whether rich full-history payload mode is enabled.
        /// </summary>
        public bool UseFullHistoryPayload { get; set; }

        /// <summary>
        /// Gets or sets whether conversation history is eligible.
        /// </summary>
        public bool IncludeConversationHistory { get; set; } = true;

        /// <summary>
        /// Gets or sets whether tool calls and results are eligible.
        /// </summary>
        public bool IncludeToolCalls { get; set; }

        /// <summary>
        /// Gets or sets whether detailed tool metadata is eligible.
        /// </summary>
        public bool IncludeToolMetadata { get; set; }

        /// <summary>
        /// Gets or sets whether general metadata is eligible.
        /// </summary>
        public bool IncludeMetadata { get; set; }

        /// <summary>
        /// Gets or sets whether manual history selection is available.
        /// </summary>
        public bool AllowManualHistorySelection { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum history messages allowed by the profile.
        /// </summary>
        public int? MaxHistoryMessages { get; set; }

        /// <summary>
        /// Gets or sets an optional output reserve override.
        /// </summary>
        public int? DefaultOutputReserveOverride { get; set; }

        /// <summary>
        /// Gets or sets profile metadata.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
