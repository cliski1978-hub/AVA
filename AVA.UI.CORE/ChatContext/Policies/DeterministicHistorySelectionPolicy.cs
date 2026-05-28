using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Policies
{
    /// <summary>
    /// Deterministic model-aware history selection.
    /// Produces a ContextSelectionResult — does NOT assemble the final prompt.
    /// Newest messages are preserved; oldest are dropped first when budget is tight.
    /// All candidate messages appear in Items (included or excluded) for future UI preview.
    /// </summary>
    public class DeterministicHistorySelectionPolicy : IHistorySelectionPolicy
    {
        private readonly ITokenEstimator _tokenEstimator;
        private readonly IContextBudgetCalculator _budgetCalculator;

        public DeterministicHistorySelectionPolicy(
            ITokenEstimator tokenEstimator,
            IContextBudgetCalculator budgetCalculator)
        {
            _tokenEstimator   = tokenEstimator;
            _budgetCalculator = budgetCalculator;
        }

        public ContextSelectionResult SelectHistory(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings)
        {
            sessionId     = sessionId     ?? string.Empty;
            modelId       = modelId       ?? string.Empty;
            currentPrompt = currentPrompt ?? string.Empty;
            modelSettings = modelSettings ?? new ModelContextSettings();

            var history  = (chatHistory ?? Enumerable.Empty<SessionChatMessage>())
                               .OrderBy(m => m.Timestamp)
                               .ToList();
            var warnings = new List<string>();

            var contextWindow = modelSettings.ContextWindow;
            var outputReserve = modelSettings.DefaultOutputReserve;

            if (contextWindow <= 0)
            {
                warnings.Add("Model context window is missing or zero.");
                contextWindow = 8192;
            }
            if (outputReserve >= contextWindow)
            {
                warnings.Add("Output reserve exceeds context window.");
                outputReserve = contextWindow / 4;
            }

            var promptTokens  = _tokenEstimator.EstimateTokens(currentPrompt);
            var historyBudget = contextWindow - outputReserve - promptTokens;

            if (promptTokens > contextWindow - outputReserve)
                warnings.Add("Current prompt exceeds available context.");

            var promptItem = new PromptContextItem
            {
                ItemId          = "current-prompt",
                ItemType        = PromptContextItemType.CurrentPrompt,
                SourceId        = "current-prompt",
                SourceLabel     = "Current Prompt",
                Content         = currentPrompt,
                EstimatedTokens = promptTokens,
                IsIncluded      = true,
                SelectionStatus = PromptContextSelectionStatus.Required
            };

            var candidates   = ApplyHistoryPolicy(history, modelSettings);
            var candidateSet = new HashSet<string>(
                candidates.Select(m => m.MessageId), StringComparer.OrdinalIgnoreCase);

            var includedSet       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roleFiltered      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userExcludedSet   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var policyExcludedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var msg in history.Where(m => !candidateSet.Contains(m.MessageId)))
                policyExcludedSet.Add(msg.MessageId);

            foreach (var msg in candidates.Where(m => !IsRoleEligible(m, modelSettings)))
                roleFiltered.Add(msg.MessageId);

            foreach (var msg in candidates.Where(m =>
                         !roleFiltered.Contains(m.MessageId) && m.IsExcluded))
                userExcludedSet.Add(msg.MessageId);

            var eligible = candidates
                .Where(m => !roleFiltered.Contains(m.MessageId) &&
                            !userExcludedSet.Contains(m.MessageId))
                .ToList();

            var pinned  = eligible.Where(m => m.IsPinned).ToList();
            var regular = eligible.Where(m => !m.IsPinned).ToList();

            var usedTokens = 0;
            foreach (var msg in pinned)
            {
                usedTokens += TokensFor(msg);
                includedSet.Add(msg.MessageId);
            }
            if (pinned.Count > 0 && usedTokens > historyBudget)
                warnings.Add("Pinned items exceed context budget.");

            for (var i = regular.Count - 1; i >= 0; i--)
            {
                var msg = regular[i];
                var t   = TokensFor(msg);
                if (usedTokens + t <= historyBudget)
                {
                    usedTokens += t;
                    includedSet.Add(msg.MessageId);
                }
            }

            var allItems = new List<PromptContextItem>(history.Count + 1);
            foreach (var msg in history)
            {
                var item = MessageToItem(msg);
                if (policyExcludedSet.Contains(msg.MessageId) ||
                    roleFiltered.Contains(msg.MessageId))
                {
                    item.IsIncluded      = false;
                    item.SelectionStatus = PromptContextSelectionStatus.ExcludedByPolicy;
                }
                else if (userExcludedSet.Contains(msg.MessageId))
                {
                    item.IsIncluded      = false;
                    item.SelectionStatus = PromptContextSelectionStatus.ExcludedByUser;
                }
                else if (includedSet.Contains(msg.MessageId))
                {
                    item.IsIncluded      = true;
                    item.IsPinned        = msg.IsPinned;
                    item.SelectionStatus = msg.IsPinned
                        ? PromptContextSelectionStatus.Pinned
                        : PromptContextSelectionStatus.Included;
                }
                else
                {
                    item.IsIncluded      = false;
                    item.SelectionStatus = PromptContextSelectionStatus.ExcludedByBudget;
                }
                allItems.Add(item);
            }
            allItems.Add(promptItem);

            var totalUsed   = allItems.Where(i => i.IsIncluded).Sum(i => i.EstimatedTokens);
            var budgetState = _budgetCalculator.CalculateBudget(
                modelId, contextWindow, outputReserve, totalUsed);

            return new ContextSelectionResult
            {
                SessionId   = sessionId,
                ModelId     = modelId,
                Items       = allItems,
                BudgetState = budgetState,
                Warnings    = warnings
            };
        }

        private static IReadOnlyList<SessionChatMessage> ApplyHistoryPolicy(
            IReadOnlyList<SessionChatMessage> history, ModelContextSettings s)
        {
            if (history.Count == 0) return history;
            return s.HistoryPolicy switch
            {
                HistoryPolicyType.NoHistory          => Array.Empty<SessionChatMessage>(),
                HistoryPolicyType.LatestOnly         => history.TakeLast(1).ToList(),
                HistoryPolicyType.ManualOnly         => history.Where(m => m.IsPinned).ToList(),
                HistoryPolicyType.CurrentDay         => history
                    .Where(m => m.Timestamp.Date == DateTime.UtcNow.Date).ToList(),
                HistoryPolicyType.RecentMessages     => s.MaxHistoryMessages.HasValue
                    ? history.TakeLast(s.MaxHistoryMessages.Value).ToList()
                    : (IReadOnlyList<SessionChatMessage>)history,
                HistoryPolicyType.MinimalConversation => history
                    .TakeLast(Math.Max(1, s.MaxHistoryMessages ?? 2)).ToList(),
                _                                    => history
            };
        }

        private static bool IsRoleEligible(SessionChatMessage m, ModelContextSettings s) =>
            m.Role switch
            {
                ChatMessageRole.User       => s.IncludeConversationHistory,
                ChatMessageRole.Assistant  => s.IncludeConversationHistory,
                ChatMessageRole.System     => s.IncludeConversationHistory,
                ChatMessageRole.ToolCall   => s.IncludeToolCalls,
                ChatMessageRole.ToolResult => s.IncludeToolCalls,
                ChatMessageRole.Metadata   => s.IncludeMetadata,
                _                          => s.IncludeConversationHistory
            };

        private PromptContextItem MessageToItem(SessionChatMessage msg) => new()
        {
            ItemId          = msg.MessageId,
            ItemType        = RoleToItemType(msg.Role),
            SourceId        = msg.MessageId,
            SourceLabel     = msg.Role.ToString(),
            Content         = msg.Content,
            EstimatedTokens = TokensFor(msg),
            IsPinned        = msg.IsPinned,
            IsUserOverride  = msg.IsExcluded,
            Metadata        = new Dictionary<string, string>(msg.Metadata)
        };

        private static PromptContextItemType RoleToItemType(ChatMessageRole role) => role switch
        {
            ChatMessageRole.System     => PromptContextItemType.SystemInstruction,
            ChatMessageRole.ToolCall   => PromptContextItemType.ToolCall,
            ChatMessageRole.ToolResult => PromptContextItemType.ToolResult,
            ChatMessageRole.Metadata   => PromptContextItemType.Metadata,
            _                          => PromptContextItemType.ChatMessage
        };

        private int TokensFor(SessionChatMessage msg) =>
            msg.EstimatedTokens > 0
                ? msg.EstimatedTokens
                : _tokenEstimator.EstimateTokens(msg.Content);
    }
}
