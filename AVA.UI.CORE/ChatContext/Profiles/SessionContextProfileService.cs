using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Profiles
{
    /// <summary>
    /// Provides built-in deterministic session context profiles.
    /// </summary>
    public class SessionContextProfileService : ISessionContextProfileService
    {
        private readonly IReadOnlyList<SessionContextProfile> _profiles;

        /// <summary>
        /// Initializes the built-in session context profile service.
        /// </summary>
        public SessionContextProfileService()
        {
            _profiles = BuildProfiles();
        }

        /// <inheritdoc />
        public IReadOnlyList<SessionContextProfile> GetDefaultProfiles() =>
            _profiles.Select(CloneProfile).ToList();

        /// <inheritdoc />
        public SessionContextProfile? GetProfile(SessionContextProfileType profileType) =>
            _profiles.FirstOrDefault(p => p.ProfileType == profileType) is { } profile
                ? CloneProfile(profile)
                : null;

        /// <inheritdoc />
        public ModelContextSettings ApplyProfile(
            ModelContextSettings baseSettings,
            SessionContextProfile profile)
        {
            baseSettings ??= new ModelContextSettings();
            profile ??= GetProfile(SessionContextProfileType.Default) ?? new SessionContextProfile();

            var applied = CloneSettings(baseSettings);
            var effectiveProfile = AdjustForInternalMemory(applied, profile);

            applied.HistoryPolicy = effectiveProfile.HistoryPolicy;
            applied.UseFullHistoryPayload = effectiveProfile.UseFullHistoryPayload;
            applied.IncludeConversationHistory = effectiveProfile.IncludeConversationHistory;
            applied.IncludeToolCalls = effectiveProfile.IncludeToolCalls;
            applied.IncludeToolMetadata = effectiveProfile.IncludeToolMetadata;
            applied.IncludeMetadata = effectiveProfile.IncludeMetadata;
            applied.AllowManualHistorySelection = effectiveProfile.AllowManualHistorySelection;
            applied.MaxHistoryMessages = effectiveProfile.MaxHistoryMessages;

            if (effectiveProfile.DefaultOutputReserveOverride.HasValue)
                applied.DefaultOutputReserve = Math.Max(0, effectiveProfile.DefaultOutputReserveOverride.Value);

            return applied;
        }

        private static SessionContextProfile AdjustForInternalMemory(
            ModelContextSettings baseSettings,
            SessionContextProfile profile)
        {
            var adjusted = CloneProfile(profile);
            if (!baseSettings.SupportsInternalMemory)
                return adjusted;

            if (adjusted.ProfileType is SessionContextProfileType.FullRecall or SessionContextProfileType.DeepReasoning)
                return adjusted;

            adjusted.UseFullHistoryPayload = false;
            adjusted.HistoryPolicy = adjusted.HistoryPolicy == HistoryPolicyType.FullSession
                ? HistoryPolicyType.LatestOnly
                : adjusted.HistoryPolicy;

            return adjusted;
        }

        private static ModelContextSettings CloneSettings(ModelContextSettings src) => new()
        {
            ModelId = src.ModelId,
            ContextWindow = src.ContextWindow,
            DefaultOutputReserve = src.DefaultOutputReserve,
            HistoryPolicy = src.HistoryPolicy,
            UseFullHistoryPayload = src.UseFullHistoryPayload,
            IncludeConversationHistory = src.IncludeConversationHistory,
            IncludeToolCalls = src.IncludeToolCalls,
            IncludeToolMetadata = src.IncludeToolMetadata,
            IncludeMetadata = src.IncludeMetadata,
            SupportsInternalMemory = src.SupportsInternalMemory,
            MaxHistoryMessages = src.MaxHistoryMessages,
            AllowManualHistorySelection = src.AllowManualHistorySelection
        };

        private static SessionContextProfile CloneProfile(SessionContextProfile src) => new()
        {
            ProfileId = src.ProfileId,
            Name = src.Name,
            Description = src.Description,
            ProfileType = src.ProfileType,
            HistoryPolicy = src.HistoryPolicy,
            UseFullHistoryPayload = src.UseFullHistoryPayload,
            IncludeConversationHistory = src.IncludeConversationHistory,
            IncludeToolCalls = src.IncludeToolCalls,
            IncludeToolMetadata = src.IncludeToolMetadata,
            IncludeMetadata = src.IncludeMetadata,
            AllowManualHistorySelection = src.AllowManualHistorySelection,
            MaxHistoryMessages = src.MaxHistoryMessages,
            DefaultOutputReserveOverride = src.DefaultOutputReserveOverride,
            Metadata = new Dictionary<string, string>(src.Metadata)
        };

        private static IReadOnlyList<SessionContextProfile> BuildProfiles() => new List<SessionContextProfile>
        {
            new()
            {
                ProfileId = "default",
                Name = "Default",
                Description = "Balanced general-purpose behavior.",
                ProfileType = SessionContextProfileType.Default,
                HistoryPolicy = HistoryPolicyType.RecentMessages,
                UseFullHistoryPayload = false,
                IncludeConversationHistory = true,
                IncludeToolCalls = false,
                IncludeToolMetadata = false,
                IncludeMetadata = false,
                AllowManualHistorySelection = true
            },
            new()
            {
                ProfileId = "coding",
                Name = "Coding Mode",
                Description = "Keeps coding context, tool call visibility, and recent reasoning available.",
                ProfileType = SessionContextProfileType.Coding,
                HistoryPolicy = HistoryPolicyType.RecentMessages,
                UseFullHistoryPayload = true,
                IncludeConversationHistory = true,
                IncludeToolCalls = true,
                IncludeToolMetadata = true,
                IncludeMetadata = true,
                AllowManualHistorySelection = true,
                MaxHistoryMessages = 30
            },
            new()
            {
                ProfileId = "minimal",
                Name = "Minimal Mode",
                Description = "Small prompt footprint.",
                ProfileType = SessionContextProfileType.Minimal,
                HistoryPolicy = HistoryPolicyType.LatestOnly,
                UseFullHistoryPayload = false,
                IncludeConversationHistory = true,
                IncludeToolCalls = false,
                IncludeToolMetadata = false,
                IncludeMetadata = false,
                AllowManualHistorySelection = true,
                MaxHistoryMessages = 1
            },
            new()
            {
                ProfileId = "tool-router",
                Name = "Tool Router Mode",
                Description = "For specialized models that mainly route, choose, or execute tools.",
                ProfileType = SessionContextProfileType.ToolRouter,
                HistoryPolicy = HistoryPolicyType.MinimalConversation,
                UseFullHistoryPayload = false,
                IncludeConversationHistory = false,
                IncludeToolCalls = true,
                IncludeToolMetadata = true,
                IncludeMetadata = true,
                AllowManualHistorySelection = true,
                MaxHistoryMessages = 2
            },
            new()
            {
                ProfileId = "deep-reasoning",
                Name = "Deep Reasoning Mode",
                Description = "Provides broader conversational continuity and enough metadata for complex reasoning.",
                ProfileType = SessionContextProfileType.DeepReasoning,
                HistoryPolicy = HistoryPolicyType.FullSession,
                UseFullHistoryPayload = true,
                IncludeConversationHistory = true,
                IncludeToolCalls = true,
                IncludeToolMetadata = false,
                IncludeMetadata = true,
                AllowManualHistorySelection = true
            },
            new()
            {
                ProfileId = "full-recall",
                Name = "Full Recall Mode",
                Description = "Maximum session recall when context window allows it.",
                ProfileType = SessionContextProfileType.FullRecall,
                HistoryPolicy = HistoryPolicyType.FullSession,
                UseFullHistoryPayload = true,
                IncludeConversationHistory = true,
                IncludeToolCalls = true,
                IncludeToolMetadata = true,
                IncludeMetadata = true,
                AllowManualHistorySelection = true
            },
            new()
            {
                ProfileId = "manual",
                Name = "Manual Mode",
                Description = "User explicitly selects context items.",
                ProfileType = SessionContextProfileType.Manual,
                HistoryPolicy = HistoryPolicyType.ManualOnly,
                UseFullHistoryPayload = false,
                IncludeConversationHistory = true,
                IncludeToolCalls = true,
                IncludeToolMetadata = false,
                IncludeMetadata = false,
                AllowManualHistorySelection = true
            },
            new()
            {
                ProfileId = "custom",
                Name = "Custom",
                Description = "Manual adjustments after selecting a profile.",
                ProfileType = SessionContextProfileType.Custom,
                HistoryPolicy = HistoryPolicyType.RecentMessages,
                UseFullHistoryPayload = false,
                IncludeConversationHistory = true,
                AllowManualHistorySelection = true
            }
        };
    }
}
