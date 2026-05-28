using System.Text;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Utilities;

namespace AVA.UI.CORE.ChatContext.Offload
{
    /// <summary>
    /// Writes selected chat history to structured searchable JSON files.
    /// </summary>
    public class ChatContextOffloadService : IChatContextOffloadService
    {
        private const string RootPath = "internal_memory/runtime/chat_offloads";

        /// <inheritdoc />
        public async Task<string> OffloadAsync(
            string sessionId,
            string? vaultId,
            string? projectId,
            IEnumerable<SessionChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            sessionId = string.IsNullOrWhiteSpace(sessionId) ? "unknown-session" : sessionId.Trim();

            var selectedMessages = (messages ?? Enumerable.Empty<SessionChatMessage>())
                .Where(m => m != null)
                .ToList();

            if (selectedMessages.Count == 0)
                return string.Empty;

            var createdAt = DateTime.UtcNow;
            var package = BuildPackage(sessionId, vaultId, projectId, selectedMessages, createdAt);
            var outputDirectory = BuildOutputDirectory(vaultId, projectId, sessionId);
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new InvalidOperationException("Failed to create output directory.", ex);
            }

            var outputPath = Path.Combine(outputDirectory, $"chat_offload_{createdAt:yyyyMMdd_HHmmss}.json");
            var json = ChatOffloadFormatter.ToJson(package);

            try
            {
                await File.WriteAllTextAsync(outputPath, json, new UTF8Encoding(false), cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new InvalidOperationException("Failed to write offload file.", ex);
            }

            return outputPath;
        }

        private static ChatOffloadPackage BuildPackage(
            string sessionId,
            string? vaultId,
            string? projectId,
            IReadOnlyCollection<SessionChatMessage> messages,
            DateTime createdAt)
        {
            var exportedMessages = messages
                .OrderBy(m => m.Timestamp)
                .Select(ToOffloadMessage)
                .ToList();

            var package = new ChatOffloadPackage
            {
                SessionId = sessionId,
                VaultId = string.IsNullOrWhiteSpace(vaultId) ? null : vaultId,
                ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId,
                CreatedAt = createdAt,
                Messages = exportedMessages
            };

            package.Metadata["MessageCount"] = exportedMessages.Count.ToString();
            package.Metadata["IncludedTokenEstimate"] = exportedMessages.Sum(m => m.EstimatedTokens).ToString();
            package.Metadata["ExportedBy"] = "AVA.UI.ChatContext";
            package.Metadata["SelectedModelId"] = string.Join(
                ",",
                exportedMessages
                    .Select(m => m.ModelId)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            ApplyContextMetadata(package, messages);

            return package;
        }

        private static ChatOffloadMessage ToOffloadMessage(SessionChatMessage message)
        {
            return new ChatOffloadMessage
            {
                MessageId = message.MessageId ?? string.Empty,
                Role = message.Role,
                Timestamp = message.Timestamp,
                Content = message.Content ?? string.Empty,
                ModelId = message.ModelId,
                EstimatedTokens = Math.Max(0, message.EstimatedTokens),
                WasPinned = message.IsPinned,
                Metadata = new Dictionary<string, string>(message.Metadata ?? new Dictionary<string, string>())
            };
        }

        private static string BuildOutputDirectory(string? vaultId, string? projectId, string sessionId)
        {
            var parts = new[]
            {
                RootPath,
                SafeSegment(vaultId, "no-vault"),
                SafeSegment(projectId, "no-project"),
                SafeSegment(sessionId, "unknown-session")
            };

            return Path.GetFullPath(Path.Combine(parts));
        }

        private static string SafeSegment(string? value, string fallback)
        {
            var segment = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
                segment = segment.Replace(invalid, '_');

            return segment;
        }

        private static void ApplyContextMetadata(
            ChatOffloadPackage package,
            IEnumerable<SessionChatMessage> messages)
        {
            var sourceMetadata = messages
                .Select(m => m.Metadata)
                .FirstOrDefault(m => m != null && m.Keys.Any(k => k.StartsWith("Offload.", StringComparison.OrdinalIgnoreCase)));

            if (sourceMetadata == null)
                return;

            foreach (var pair in sourceMetadata.Where(p => p.Key.StartsWith("Offload.", StringComparison.OrdinalIgnoreCase)))
            {
                var key = pair.Key["Offload.".Length..];
                if (!string.IsNullOrWhiteSpace(key))
                    package.Metadata[key] = pair.Value;
            }
        }
    }
}
