using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models.Query;

namespace AVA.Vault.Core.Services.Queries
{
    /// <summary>
    /// Provides access to Vault-level audit trails, including queryable logs and system events.
    /// Supports both database-backed and file-based log retrieval.
    /// </summary>
    public sealed class VaultAuditQueries
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;
        private readonly string _vaultRoot;

        public VaultAuditQueries(IVaultDbContext db, VaultLogger logger, string vaultRoot)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vaultRoot = vaultRoot ?? throw new ArgumentNullException(nameof(vaultRoot));
        }

        // -------------------------------------------------------------
        // Database Audit Retrieval (if implemented)
        // -------------------------------------------------------------

        /// <summary>
        /// Retrieves audit log entries from the database (if available) filtered by optional parameters.
        /// </summary>
        public async Task<List<VaultAuditEntry>> GetAuditEntriesAsync(
            string? source = null,
            string? level = null,
            DateTime? since = null,
            int limit = 500,
            CancellationToken ct = default)
        {
            try
            {
                if (!_db.DbContext.Database.CanConnect())
                {
                    _logger.Log("VaultAuditQueries", "Database unavailable, falling back to file logs.");
                    return await GetFileAuditEntriesAsync(source, level, since, limit, ct);
                }

                var query = _db.Set<VaultAuditEntry>().AsQueryable();

                if (!string.IsNullOrEmpty(source))
                    query = query.Where(a => a.Source == source);

                if (!string.IsNullOrEmpty(level))
                    query = query.Where(a => a.Level == level);

                if (since.HasValue)
                    query = query.Where(a => a.Timestamp >= since.Value);

                var results = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .AsNoTracking()
                    .ToListAsync(ct);

                _logger.Log("VaultAuditQueries", $"Retrieved {results.Count} audit entries from DB.");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultAuditQueries", "Error retrieving DB audit entries.", ex);
                return await GetFileAuditEntriesAsync(source, level, since, limit, ct);
            }
        }

        // -------------------------------------------------------------
        // File-Based Log Retrieval
        // -------------------------------------------------------------

        /// <summary>
        /// Reads recent log entries from the Vault’s log folder (default “logs/vault.log”).
        /// </summary>
        public async Task<List<VaultAuditEntry>> GetFileAuditEntriesAsync(
            string? source = null,
            string? level = null,
            DateTime? since = null,
            int limit = 500,
            CancellationToken ct = default)
        {
            var logDir = Path.Combine(_vaultRoot, "logs");
            var logFile = Path.Combine(logDir, "vault.log");
            var entries = new List<VaultAuditEntry>();

            if (!File.Exists(logFile))
                return entries;

            try
            {
                var lines = await File.ReadAllLinesAsync(logFile, ct);
                foreach (var line in lines.Reverse()) // read newest first
                {
                    if (entries.Count >= limit) break;
                    var parsed = ParseLogLine(line);
                    if (parsed == null) continue;

                    if (!string.IsNullOrEmpty(source) && parsed.Source != source)
                        continue;
                    if (!string.IsNullOrEmpty(level) && !parsed.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (since.HasValue && parsed.Timestamp < since.Value)
                        continue;

                    entries.Add(parsed);
                }

                _logger.Log("VaultAuditQueries", $"Retrieved {entries.Count} audit entries from file.");
                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultAuditQueries", "Error reading file audit logs.", ex);
                return entries;
            }
        }

        // -------------------------------------------------------------
        // Statistics and Summaries
        // -------------------------------------------------------------

        /// <summary>
        /// Returns aggregated statistics about recent log activity.
        /// </summary>
        public async Task<VaultAuditSummary> GetAuditSummaryAsync(CancellationToken ct = default)
        {
            var entries = await GetFileAuditEntriesAsync(limit: 1000, ct: ct);

            var summary = new VaultAuditSummary
            {
                TotalEntries = entries.Count,
                ErrorCount = entries.Count(e => e.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)),
                WarningCount = entries.Count(e => e.Level.Equals("Warning", StringComparison.OrdinalIgnoreCase)),
                InfoCount = entries.Count(e => e.Level.Equals("Info", StringComparison.OrdinalIgnoreCase)),
                Sources = entries.GroupBy(e => e.Source)
                                 .ToDictionary(g => g.Key, g => g.Count())
            };

            return summary;
        }

        // -------------------------------------------------------------
        // Helper Methods
        // -------------------------------------------------------------

        private static VaultAuditEntry? ParseLogLine(string line)
        {
            try
            {
                // Expected format: [2025-11-08 09:12:34Z] [Level] [Source] Message
                if (!line.StartsWith("[")) return null;
                var parts = line.Split(']', 4, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) return null;

                var timestamp = DateTime.Parse(parts[0].Trim('[', ']').Trim());
                var level = parts[1].Trim('[', ']').Trim();
                var source = parts[2].Trim('[', ']').Trim();
                var message = parts.Length > 3 ? parts[3].Trim() : string.Empty;

                return new VaultAuditEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Source = source,
                    Message = message
                };
            }
            catch
            {
                return null;
            }
        }
    }

    // -------------------------------------------------------------
    // Data Models for Audit Queries
    // -------------------------------------------------------------

    public sealed class VaultAuditEntry
    {
        public string ID { get; set; } = Guid.NewGuid().ToString("N");
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public sealed class VaultAuditSummary
    {
        public int TotalEntries { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public Dictionary<string, int> Sources { get; set; } = new();
    }
}
