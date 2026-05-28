using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Session-specific runtime prompt context behavior for one model binding.
    /// </summary>
    public class RuntimeContextSettings
    {
        /// <summary>History selection policy used by this session/model binding.</summary>
        public HistoryPolicyType HistoryPolicy { get; set; } = HistoryPolicyType.RecentMessages;

        /// <summary>When true, rich full-history payload mode is enabled for this session/model binding.</summary>
        public bool UseFullHistoryPayload { get; set; }

        /// <summary>When true, conversation history is eligible for prompt inclusion.</summary>
        public bool IncludeConversationHistory { get; set; } = true;

        /// <summary>When true, tool call and tool result records are eligible for prompt inclusion.</summary>
        public bool IncludeToolCalls { get; set; }

        /// <summary>When true, detailed tool execution metadata is eligible for prompt inclusion.</summary>
        public bool IncludeToolMetadata { get; set; }

        /// <summary>When true, general metadata is eligible for prompt inclusion.</summary>
        public bool IncludeMetadata { get; set; }

        /// <summary>When true, deterministic automatic compression can be applied.</summary>
        public bool EnableAutomaticCompression { get; set; }

        /// <summary>When true, externally retrieved memory may be injected.</summary>
        public bool EnableMemoryInjection { get; set; }

        /// <summary>Optional deterministic cap on included history messages.</summary>
        public int? MaxHistoryMessages { get; set; }

        /// <summary>When true, the user may manually override context item inclusion.</summary>
        public bool AllowManualHistorySelection { get; set; } = true;
    }
}
