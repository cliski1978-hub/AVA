using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Monitoring
{
    /// <summary>
    /// Provides deterministic context-window usage analysis for prompt context packages.
    /// </summary>
    public class ContextUsageMonitor : IContextUsageMonitor
    {
        private const double NearLimitThreshold = 85.0;

        /// <inheritdoc />
        public ContextUsageMetrics Calculate(
            PromptContextPackage package,
            ModelContextSettings modelSettings)
        {
            package ??= new PromptContextPackage();
            modelSettings ??= new ModelContextSettings();

            var includedItems = package.IncludedItems ?? new List<PromptContextItem>();
            var contextWindow = Math.Max(0, modelSettings.ContextWindow);
            var reservedOutput = Math.Max(0, modelSettings.DefaultOutputReserve);
            var usedTokens = includedItems.Sum(i => Math.Max(0, i?.EstimatedTokens ?? 0));
            var reservedTotal = usedTokens + reservedOutput;
            var remaining = contextWindow - reservedTotal;
            var usagePercent = CalculateUsagePercent(reservedTotal, contextWindow);
            var excludedCount = TryReadInt(package.Metadata, "ExcludedItemCount");

            var metrics = new ContextUsageMetrics
            {
                ModelId = package.ModelId ?? modelSettings.ModelId ?? string.Empty,
                ContextWindow = contextWindow,
                UsedTokens = usedTokens,
                ReservedOutputTokens = reservedOutput,
                RemainingTokens = remaining,
                UsagePercent = usagePercent,
                IsOverBudget = remaining < 0,
                IsNearLimit = usagePercent >= NearLimitThreshold,
                EstimatedResponseCapacity = Math.Max(0, remaining),
                IncludedItemCount = includedItems.Count,
                ExcludedItemCount = Math.Max(0, excludedCount),
                CategoryTokenBreakdown = BuildCategoryBreakdown(includedItems)
            };

            ApplyWarnings(metrics, modelSettings);
            return metrics;
        }

        private static double CalculateUsagePercent(int reservedTotal, int contextWindow)
        {
            if (contextWindow <= 0)
                return reservedTotal > 0 ? 100.0 : 0.0;

            var pct = (double)reservedTotal / contextWindow * 100.0;
            if (double.IsNaN(pct) || double.IsInfinity(pct))
                return 0.0;

            return Math.Max(0.0, pct);
        }

        private static Dictionary<string, int> BuildCategoryBreakdown(IEnumerable<PromptContextItem> items)
        {
            var categories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Conversation"] = 0,
                ["System"] = 0,
                ["ToolCalls"] = 0,
                ["ToolResults"] = 0,
                ["Metadata"] = 0,
                ["CurrentPrompt"] = 0,
                ["Memory"] = 0,
                ["Other"] = 0
            };

            foreach (var item in items ?? Enumerable.Empty<PromptContextItem>())
            {
                var key = ToCategory(item.ItemType);
                categories[key] += Math.Max(0, item.EstimatedTokens);
            }

            return categories;
        }

        private static string ToCategory(PromptContextItemType itemType) => itemType switch
        {
            PromptContextItemType.ChatMessage => "Conversation",
            PromptContextItemType.SystemInstruction => "System",
            PromptContextItemType.ToolCall => "ToolCalls",
            PromptContextItemType.ToolResult => "ToolResults",
            PromptContextItemType.Metadata => "Metadata",
            PromptContextItemType.CurrentPrompt => "CurrentPrompt",
            PromptContextItemType.MemoryInjection => "Memory",
            _ => "Other"
        };

        private static void ApplyWarnings(ContextUsageMetrics metrics, ModelContextSettings modelSettings)
        {
            if (metrics.ContextWindow <= 0)
            {
                metrics.Warnings.Add(new ContextUsageWarning
                {
                    Code = "MissingContextWindow",
                    Message = "Model context window is missing or invalid.",
                    Severity = "Critical"
                });
            }

            if (metrics.IsOverBudget)
            {
                metrics.Warnings.Add(new ContextUsageWarning
                {
                    Code = "OverBudget",
                    Message = "Prompt exceeds model context window.",
                    Severity = "Critical"
                });
            }

            if (metrics.IsNearLimit && !metrics.IsOverBudget)
            {
                metrics.Warnings.Add(new ContextUsageWarning
                {
                    Code = "NearLimit",
                    Message = "Prompt is approaching context-window capacity.",
                    Severity = "Warning"
                });
            }

            var lowCapacityThreshold = Math.Max(1, Math.Max(0, modelSettings.DefaultOutputReserve) / 2);
            if (!metrics.IsOverBudget && metrics.EstimatedResponseCapacity < lowCapacityThreshold)
            {
                metrics.Warnings.Add(new ContextUsageWarning
                {
                    Code = "ResponseCapacityLow",
                    Message = "Remaining response capacity is low.",
                    Severity = "Warning"
                });
            }
        }

        private static int TryReadInt(Dictionary<string, string>? metadata, string key)
        {
            if (metadata != null &&
                metadata.TryGetValue(key, out var value) &&
                int.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return 0;
        }
    }
}
