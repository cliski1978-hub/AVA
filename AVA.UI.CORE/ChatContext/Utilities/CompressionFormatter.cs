using System.Globalization;
using System.Text;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Utilities
{
    /// <summary>
    /// Formats deterministic compressed context summary blocks.
    /// </summary>
    public static class CompressionFormatter
    {
        private const int PreviewLength = 180;

        /// <summary>
        /// Builds a readable summary text from chronological source items.
        /// </summary>
        public static string BuildSummaryText(IReadOnlyList<PromptContextItem> items, DateTime createdAt)
        {
            var safeItems = items ?? Array.Empty<PromptContextItem>();
            var builder = new StringBuilder();
            builder.AppendLine($"Created: {createdAt.ToString("O", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Original Items: {safeItems.Count}");

            foreach (var item in safeItems)
            {
                var label = string.IsNullOrWhiteSpace(item.SourceLabel)
                    ? item.ItemType.ToString()
                    : item.SourceLabel;
                var preview = BuildPreview(item.Content);
                if (string.IsNullOrWhiteSpace(preview))
                    preview = "(empty content)";

                builder.AppendLine($"- {label}: {preview}");
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats a compressed context block for prompt assembly.
        /// </summary>
        public static string FormatBlock(CompressedContextBlock block)
        {
            if (block == null)
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine("=== COMPRESSED CONVERSATION SUMMARY ===");
            builder.AppendLine($"Time Range: {block.SourceStartTimestamp.ToString("O", CultureInfo.InvariantCulture)} to {block.SourceEndTimestamp.ToString("O", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Original Messages: {block.OriginalMessageCount}");
            builder.AppendLine($"Original Tokens: {block.OriginalEstimatedTokens}");
            builder.AppendLine($"Compressed Tokens: {block.CompressedEstimatedTokens}");
            builder.AppendLine();
            builder.AppendLine("Summary:");
            builder.AppendLine(block.SummaryText ?? string.Empty);
            return builder.ToString().TrimEnd();
        }

        private static string BuildPreview(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var normalized = string.Join(" ", content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var sentenceEnd = normalized.IndexOfAny(new[] { '.', '!', '?' });
            if (sentenceEnd >= 0 && sentenceEnd < PreviewLength)
                normalized = normalized[..(sentenceEnd + 1)];

            if (normalized.Length <= PreviewLength)
                return normalized;

            return normalized[..PreviewLength].TrimEnd() + "...";
        }
    }
}
