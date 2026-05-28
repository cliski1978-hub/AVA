using System.Text;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Debugging
{
    /// <summary>
    /// Builds inspectable prompt debug packages from assembled prompt context packages.
    /// </summary>
    public class PromptDebugFormatter : IPromptDebugFormatter
    {
        private static readonly PromptContextItemType[] SectionOrder =
        [
            PromptContextItemType.SystemInstruction,
            PromptContextItemType.MemoryInjection,
            PromptContextItemType.ChatMessage,
            PromptContextItemType.ToolCall,
            PromptContextItemType.ToolResult,
            PromptContextItemType.Metadata,
            PromptContextItemType.CurrentPrompt,
            PromptContextItemType.Other
        ];

        /// <inheritdoc />
        public PromptDebugPackage BuildDebugPackage(
            PromptContextPackage package,
            ModelContextSettings modelSettings)
        {
            package ??= new PromptContextPackage();
            modelSettings ??= new ModelContextSettings();

            var items = package.IncludedItems ?? new List<PromptContextItem>();
            var sections = BuildSections(items);
            var fullPrompt = BuildFullPromptText(sections);
            var metadata = new Dictionary<string, string>(package.Metadata ?? new Dictionary<string, string>())
            {
                ["ContextWindow"] = Math.Max(0, modelSettings.ContextWindow).ToString(),
                ["OutputReserve"] = Math.Max(0, modelSettings.DefaultOutputReserve).ToString(),
                ["SectionCount"] = sections.Count.ToString()
            };

            return new PromptDebugPackage
            {
                SessionId = package.SessionId ?? string.Empty,
                ModelId = package.ModelId ?? modelSettings.ModelId ?? string.Empty,
                GeneratedAt = DateTime.UtcNow,
                FullPromptText = fullPrompt,
                TotalEstimatedTokens = sections.Sum(s => Math.Max(0, s.EstimatedTokens)),
                Sections = sections,
                Metadata = metadata
            };
        }

        private static List<PromptDebugSection> BuildSections(IReadOnlyList<PromptContextItem> orderedItems)
        {
            var sections = new List<PromptDebugSection>();

            foreach (var itemType in SectionOrder)
            {
                var items = orderedItems
                    .Where(i => i != null && NormalizeType(i.ItemType) == itemType)
                    .Select(CopyItem)
                    .ToList();

                if (items.Count == 0)
                    continue;

                sections.Add(new PromptDebugSection
                {
                    SectionId = ToSectionId(itemType),
                    Title = ToSectionTitle(itemType),
                    ItemType = itemType,
                    EstimatedTokens = items.Sum(i => Math.Max(0, i.EstimatedTokens)),
                    IsCollapsed = itemType is PromptContextItemType.ToolCall or PromptContextItemType.ToolResult or PromptContextItemType.Metadata,
                    Content = BuildSectionContent(items),
                    Items = items
                });
            }

            return sections;
        }

        private static string BuildFullPromptText(IEnumerable<PromptDebugSection> sections)
        {
            var builder = new StringBuilder();
            foreach (var section in sections)
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.AppendLine($"=== {section.Title.ToUpperInvariant()} ===");
                builder.AppendLine(section.Content ?? string.Empty);
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildSectionContent(IEnumerable<PromptContextItem> items)
        {
            var builder = new StringBuilder();
            foreach (var item in items)
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.AppendLine($"[{ToItemLabel(item)}]");
                builder.AppendLine(item.Content ?? string.Empty);
            }

            return builder.ToString().TrimEnd();
        }

        private static string ToItemLabel(PromptContextItem item)
        {
            var source = string.IsNullOrWhiteSpace(item.SourceLabel) ? item.SourceId : item.SourceLabel;
            var status = item.SelectionStatus.ToString();
            var compressed = item.Metadata != null &&
                item.Metadata.TryGetValue("Compressed", out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                    ? " | compressed"
                    : string.Empty;
            var rm = item.Metadata != null &&
                item.Metadata.TryGetValue("RMFinalWeightedScore", out var score)
                    ? $" | RM {score}"
                    : string.Empty;
            var memory = item.Metadata != null &&
                item.Metadata.TryGetValue("MemoryRelevanceScore", out var relevance)
                    ? $" | memory {relevance}"
                    : string.Empty;

            return $"{item.ItemType} | {source} | {item.EstimatedTokens} tokens | {status}{compressed}{rm}{memory}";
        }

        private static PromptContextItemType NormalizeType(PromptContextItemType itemType) =>
            SectionOrder.Contains(itemType) ? itemType : PromptContextItemType.Other;

        private static string ToSectionId(PromptContextItemType itemType) => itemType switch
        {
            PromptContextItemType.SystemInstruction => "system-instructions",
            PromptContextItemType.MemoryInjection => "memory-injections",
            PromptContextItemType.ChatMessage => "conversation-history",
            PromptContextItemType.ToolCall => "tool-calls",
            PromptContextItemType.ToolResult => "tool-results",
            PromptContextItemType.Metadata => "metadata",
            PromptContextItemType.CurrentPrompt => "current-prompt",
            _ => "other"
        };

        private static string ToSectionTitle(PromptContextItemType itemType) => itemType switch
        {
            PromptContextItemType.SystemInstruction => "System Instructions",
            PromptContextItemType.MemoryInjection => "Memory Injections",
            PromptContextItemType.ChatMessage => "Conversation History",
            PromptContextItemType.ToolCall => "Tool Calls",
            PromptContextItemType.ToolResult => "Tool Results",
            PromptContextItemType.Metadata => "Metadata",
            PromptContextItemType.CurrentPrompt => "Current Prompt",
            _ => "Other"
        };

        private static PromptContextItem CopyItem(PromptContextItem src) => new()
        {
            ItemId = src.ItemId ?? string.Empty,
            ItemType = src.ItemType,
            SourceId = src.SourceId ?? string.Empty,
            SourceLabel = src.SourceLabel ?? string.Empty,
            Content = src.Content ?? string.Empty,
            EstimatedTokens = Math.Max(0, src.EstimatedTokens),
            IsIncluded = src.IsIncluded,
            IsPinned = src.IsPinned,
            IsUserOverride = src.IsUserOverride,
            SelectionStatus = src.SelectionStatus,
            Metadata = new Dictionary<string, string>(src.Metadata ?? new Dictionary<string, string>())
        };
    }
}
