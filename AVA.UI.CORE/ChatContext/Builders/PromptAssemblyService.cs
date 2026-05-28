using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Builders
{
    /// <summary>
    /// Creates the final AVA-side PromptContextPackage from selected context items.
    /// Applies a final safety filter to enforce model payload settings.
    /// Preserves selection metadata and budget state from the selection result.
    /// Items are ordered: SystemInstruction → MemoryInjection → Chronological → CurrentPrompt last.
    /// </summary>
    public class PromptAssemblyService : IPromptAssemblyService
    {
        public PromptContextPackage Assemble(
            string sessionId,
            string modelId,
            string currentPrompt,
            ContextSelectionResult? selectionResult,
            ModelContextSettings? modelSettings)
        {
            sessionId     = sessionId     ?? string.Empty;
            modelId       = modelId       ?? string.Empty;
            currentPrompt = currentPrompt ?? string.Empty;
            modelSettings = modelSettings ?? new ModelContextSettings();

            var budgetState = selectionResult?.BudgetState ?? new PromptBudgetState
            {
                ModelId = modelId
            };

            var includedItems = selectionResult?.Items?
                .Where(i => i.IsIncluded)
                .ToList() ?? new List<PromptContextItem>();

            // Final safety filter — most filtering already done in selection policy.
            var filtered = ApplySafetyFilter(includedItems, modelSettings);

            // Ensure the current prompt item is always present.
            EnsureCurrentPrompt(filtered, currentPrompt);

            // Canonical ordering.
            var ordered = OrderItems(filtered);

            return new PromptContextPackage
            {
                SessionId     = sessionId,
                ModelId       = modelId,
                CurrentPrompt = currentPrompt,
                IncludedItems = ordered,
                BudgetState   = budgetState,
                CreatedAt     = DateTime.UtcNow,
                Metadata      = BuildMetadata(modelSettings, ordered)
            };
        }

        // ── Safety filter ─────────────────────────────────────────────────────

        private static List<PromptContextItem> ApplySafetyFilter(
            List<PromptContextItem> items, ModelContextSettings s) =>
            items.Where(item => item.ItemType switch
            {
                PromptContextItemType.ChatMessage        => s.IncludeConversationHistory,
                PromptContextItemType.SystemInstruction  => s.IncludeConversationHistory,
                PromptContextItemType.ToolCall           => s.IncludeToolCalls,
                PromptContextItemType.ToolResult         => s.IncludeToolCalls,
                PromptContextItemType.Metadata           => s.IncludeMetadata,
                PromptContextItemType.CurrentPrompt      => true,
                PromptContextItemType.MemoryInjection    => true,
                _                                        => true
            }).ToList();

        // ── Current prompt guarantee ──────────────────────────────────────────

        private static void EnsureCurrentPrompt(List<PromptContextItem> items, string currentPrompt)
        {
            if (items.Any(i => i.ItemType == PromptContextItemType.CurrentPrompt))
                return;

            items.Add(new PromptContextItem
            {
                ItemId          = "current-prompt",
                ItemType        = PromptContextItemType.CurrentPrompt,
                SourceId        = "current-prompt",
                SourceLabel     = "CurrentPrompt",
                Content         = currentPrompt,
                IsIncluded      = true,
                SelectionStatus = PromptContextSelectionStatus.Required
            });
        }

        // ── Canonical ordering ────────────────────────────────────────────────

        private static List<PromptContextItem> OrderItems(List<PromptContextItem> items)
        {
            var result = new List<PromptContextItem>(items.Count);
            result.AddRange(items.Where(i => i.ItemType == PromptContextItemType.SystemInstruction));
            result.AddRange(items.Where(i => i.ItemType == PromptContextItemType.MemoryInjection));
            result.AddRange(items.Where(i =>
                i.ItemType != PromptContextItemType.SystemInstruction &&
                i.ItemType != PromptContextItemType.MemoryInjection &&
                i.ItemType != PromptContextItemType.CurrentPrompt));
            result.AddRange(items.Where(i => i.ItemType == PromptContextItemType.CurrentPrompt));
            return result;
        }

        // ── Package metadata ──────────────────────────────────────────────────

        private static Dictionary<string, string> BuildMetadata(
            ModelContextSettings s, List<PromptContextItem> items) => new()
        {
            ["HistoryPolicy"]             = s.HistoryPolicy.ToString(),
            ["UseFullHistoryPayload"]      = s.UseFullHistoryPayload.ToString(),
            ["IncludeConversationHistory"] = s.IncludeConversationHistory.ToString(),
            ["IncludeToolCalls"]          = s.IncludeToolCalls.ToString(),
            ["IncludeToolMetadata"]       = s.IncludeToolMetadata.ToString(),
            ["IncludeMetadata"]           = s.IncludeMetadata.ToString(),
            ["SupportsInternalMemory"]    = s.SupportsInternalMemory.ToString(),
            ["ItemCount"]                 = items.Count.ToString(),
            ["IncludedTokenEstimate"]     = items.Sum(i => i.EstimatedTokens).ToString()
        };
    }
}
