using System.Globalization;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Utilities;

namespace AVA.UI.CORE.ChatContext.Compression
{
    /// <summary>
    /// Provides rule-driven compression for older prompt context items.
    /// </summary>
    public class ContextCompressionService : IContextCompressionService
    {
        private const double TriggerUsagePercent = 80.0;
        private const double TargetUsagePercent = 70.0;
        private const int RecentConversationWindow = 12;

        private readonly ITokenEstimator _tokenEstimator;

        /// <summary>
        /// Initializes a deterministic context compression service.
        /// </summary>
        public ContextCompressionService(ITokenEstimator tokenEstimator)
        {
            _tokenEstimator = tokenEstimator;
        }

        /// <summary>
        /// Compresses eligible older context items while preserving recent, pinned, and forced items.
        /// </summary>
        public CompressionResult Compress(
            IReadOnlyList<PromptContextItem> items,
            ModelContextSettings modelSettings,
            PromptBudgetState budgetState)
        {
            var safeItems = (items ?? Array.Empty<PromptContextItem>()).Select(CopyItem).ToList();
            var includedItems = safeItems.Where(i => i.IsIncluded).ToList();
            var originalTokenCount = includedItems.Sum(i => Math.Max(0, i.EstimatedTokens));

            var result = new CompressionResult
            {
                PreservedItems = includedItems.Select(CopyItem).ToList(),
                OriginalTokenCount = originalTokenCount,
                CompressedTokenCount = originalTokenCount
            };

            if (includedItems.Count == 0)
                return result;

            if (budgetState == null || budgetState.UsagePercent < TriggerUsagePercent)
                return result;

            var recentIds = includedItems
                .Where(IsRecentConversationCandidate)
                .TakeLast(RecentConversationWindow)
                .Select(i => i.ItemId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var compressible = includedItems
                .Where(i => IsCompressible(i, recentIds))
                .ToList();

            if (compressible.Count == 0)
            {
                result.Warnings.Add("Compression could not run because pinned, forced, current, or recent items consumed the eligible context.");
                return result;
            }

            var createdAt = DateTime.UtcNow;
            var summaryText = CompressionFormatter.BuildSummaryText(compressible, createdAt);
            var compressedTokens = Math.Max(1, _tokenEstimator.EstimateTokens(summaryText));
            var preservedItems = includedItems
                .Where(i => !compressible.Any(c => string.Equals(c.ItemId, i.ItemId, StringComparison.OrdinalIgnoreCase)))
                .Select(CopyItem)
                .ToList();

            var block = new CompressedContextBlock
            {
                BlockId = $"compressed-{createdAt:yyyyMMddHHmmssfff}",
                CreatedAt = createdAt,
                SourceStartTimestamp = ResolveTimestamp(compressible.FirstOrDefault(), createdAt),
                SourceEndTimestamp = ResolveTimestamp(compressible.LastOrDefault(), createdAt),
                OriginalMessageCount = compressible.Count,
                OriginalEstimatedTokens = compressible.Sum(i => Math.Max(0, i.EstimatedTokens)),
                CompressedEstimatedTokens = compressedTokens,
                SummaryText = summaryText,
                SourceMessageIds = compressible.Select(i => i.ItemId).Where(id => !string.IsNullOrWhiteSpace(id)).ToList(),
                Metadata = new Dictionary<string, string>
                {
                    ["Compressed"] = "true",
                    ["SourceMessageCount"] = compressible.Count.ToString(CultureInfo.InvariantCulture),
                    ["OriginalEstimatedTokens"] = compressible.Sum(i => Math.Max(0, i.EstimatedTokens)).ToString(CultureInfo.InvariantCulture),
                    ["CompressedEstimatedTokens"] = compressedTokens.ToString(CultureInfo.InvariantCulture)
                }
            };

            var compressedTotal = preservedItems.Sum(i => Math.Max(0, i.EstimatedTokens)) + compressedTokens;
            var tokensSaved = originalTokenCount - compressedTotal;

            if (tokensSaved <= 0)
            {
                result.Warnings.Add("Compressed summaries exceed or match the original token estimate.");
                return result;
            }

            block.Metadata["CompressionRatio"] = block.OriginalEstimatedTokens > 0
                ? ((double)block.CompressedEstimatedTokens / block.OriginalEstimatedTokens).ToString("0.###", CultureInfo.InvariantCulture)
                : "0";

            result.PreservedItems = preservedItems;
            result.CompressedBlocks = new List<CompressedContextBlock> { block };
            result.CompressedTokenCount = compressedTotal;
            result.TokensSaved = tokensSaved;
            result.CompressionApplied = true;

            var contextWindow = Math.Max(0, modelSettings?.ContextWindow ?? budgetState.ContextWindow);
            var outputReserve = Math.Max(0, modelSettings?.DefaultOutputReserve ?? budgetState.OutputReserve);
            if (contextWindow > 0)
            {
                var projectedUsage = (double)(compressedTotal + outputReserve) / contextWindow * 100.0;
                if (projectedUsage > TargetUsagePercent)
                    result.Warnings.Add("Compression could not reduce usage below the target threshold.");
            }

            return result;
        }

        private static bool IsRecentConversationCandidate(PromptContextItem item) =>
            item.ItemType == PromptContextItemType.ChatMessage ||
            item.ItemType == PromptContextItemType.ToolCall ||
            item.ItemType == PromptContextItemType.ToolResult ||
            item.ItemType == PromptContextItemType.Metadata;

        private static bool IsCompressible(PromptContextItem item, ISet<string> recentIds)
        {
            if (!item.IsIncluded || item.IsPinned || item.SelectionStatus == PromptContextSelectionStatus.ForcedByUser)
                return false;

            if (item.ItemType == PromptContextItemType.CurrentPrompt || item.ItemType == PromptContextItemType.SystemInstruction)
                return false;

            if (recentIds.Contains(item.ItemId))
                return false;

            return item.ItemType == PromptContextItemType.ChatMessage ||
                   item.ItemType == PromptContextItemType.ToolCall ||
                   item.ItemType == PromptContextItemType.ToolResult ||
                   item.ItemType == PromptContextItemType.Metadata;
        }

        private static DateTime ResolveTimestamp(PromptContextItem? item, DateTime fallback)
        {
            if (item?.Metadata == null)
                return fallback;

            foreach (var key in new[] { "Timestamp", "CreatedAt", "MessageTimestamp" })
            {
                if (item.Metadata.TryGetValue(key, out var value) &&
                    DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
                {
                    return timestamp.ToUniversalTime();
                }
            }

            return fallback;
        }

        private static PromptContextItem CopyItem(PromptContextItem src) => new()
        {
            ItemId = src.ItemId,
            ItemType = src.ItemType,
            SourceId = src.SourceId,
            SourceLabel = src.SourceLabel,
            Content = src.Content,
            EstimatedTokens = src.EstimatedTokens,
            IsIncluded = src.IsIncluded,
            IsPinned = src.IsPinned,
            IsUserOverride = src.IsUserOverride,
            SelectionStatus = src.SelectionStatus,
            Metadata = new Dictionary<string, string>(src.Metadata ?? new Dictionary<string, string>())
        };
    }
}
