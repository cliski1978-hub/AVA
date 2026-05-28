using System;
using System.IO;
using System.Text.Json;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Handles trace-level I/O logging for Vault operations.
    /// Writes structured JSONL traces to disk for later analysis.
    /// </summary>
    public static class VaultIOTraceWriter
    {
        private static readonly string TraceFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vault_trace.jsonl");

        // ?? Maintain a single internal VaultLogger instance
        private static readonly VaultLogger _logger =
            new VaultLogger(new VaultInstanceConfig { VaultID = "system-trace" });

        public static void WriteEvent(object eventData)
        {
            try
            {
                string json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.AppendAllLines(TraceFilePath, new[] { json });
                _logger.Log("VaultIOTraceWriter", $"Appended event to trace: {eventData.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultIOTraceWriter", $"Failed to write trace: {ex.Message}", ex);
            }
        }

        public static void WriteNote(MarkdownNote note)
        {
            WriteEvent(new
            {
                type = "VaultNote",
                id = note.Id,
                title = note.Title,
                content = note.Content,
                tags = note.Tags,
                created = note.Created,
                modified = note.Modified
            });
        }

        public static void WriteLink(string source, string target)
        {
            WriteEvent(new
            {
                type = "VaultLink",
                from = source,
                to = target,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
