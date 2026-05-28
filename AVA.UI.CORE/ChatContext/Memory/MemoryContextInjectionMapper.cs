using System.Globalization;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Memory
{
    /// <summary>
    /// Maps externally retrieved memory context into prompt context items.
    /// </summary>
    public static class MemoryContextInjectionMapper
    {
        /// <summary>
        /// Maps memory retrieval items into prompt context memory injection items.
        /// </summary>
        public static List<PromptContextItem> Map(MemoryRetrievalResult? result)
        {
            if (result?.Items == null || result.Items.Count == 0)
                return new List<PromptContextItem>();

            return result.Items.Select((item, index) => MapItem(item, index)).ToList();
        }

        /// <summary>
        /// Maps one memory item into a prompt context memory injection item.
        /// </summary>
        public static PromptContextItem MapItem(MemoryContextItem item)
        {
            item ??= new MemoryContextItem();
            return MapItem(item, 0);
        }

        private static PromptContextItem MapItem(MemoryContextItem item, int index)
        {
            item ??= new MemoryContextItem();
            var memoryId = string.IsNullOrWhiteSpace(item.MemoryId)
                ? $"injected-{index + 1}"
                : item.MemoryId;
            var metadata = new Dictionary<string, string>(item.Metadata ?? new Dictionary<string, string>())
            {
                ["MemoryId"] = memoryId,
                ["MemorySource"] = item.Source ?? string.Empty,
                ["MemoryRelevanceScore"] = item.RelevanceScore.ToString("0.###", CultureInfo.InvariantCulture),
                ["MemoryInjection"] = "true"
            };

            return new PromptContextItem
            {
                ItemId = $"memory-{memoryId}",
                ItemType = PromptContextItemType.MemoryInjection,
                SourceId = memoryId,
                SourceLabel = string.IsNullOrWhiteSpace(item.Title) ? item.Source ?? string.Empty : item.Title,
                Content = item.Content ?? string.Empty,
                EstimatedTokens = Math.Max(0, item.EstimatedTokens),
                IsIncluded = true,
                SelectionStatus = PromptContextSelectionStatus.Recommended,
                Metadata = metadata
            };
        }
    }
}
