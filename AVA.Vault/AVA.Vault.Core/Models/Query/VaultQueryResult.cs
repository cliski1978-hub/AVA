using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AVA.Vault.Core.Models.Query
{
    /// <summary>
    /// Represents the standardized result of a SQL query executed through the Vault Query system.
    /// Supports both typed and untyped (dictionary) results, plus timing and diagnostics metadata.
    /// </summary>
    public sealed class VaultQueryResult
    {
        /// <summary>
        /// Rows returned from the executed query, stored as key/value dictionaries for flexibility.
        /// </summary>
        [JsonPropertyName("rows")]
        public List<Dictionary<string, object>> Rows { get; set; } = new();

        /// <summary>
        /// Number of rows retrieved.
        /// </summary>
        [JsonPropertyName("rowCount")]
        public int RowCount { get; set; }

        /// <summary>
        /// Time elapsed in milliseconds for query execution (including mapping).
        /// </summary>
        [JsonPropertyName("elapsedMs")]
        public long ElapsedMs { get; set; }

        /// <summary>
        /// The raw SQL text that was executed (sanitized if user-supplied).
        /// </summary>
        [JsonPropertyName("queryText")]
        public string? QueryText { get; set; }

        /// <summary>
        /// Timestamp when the query was executed (UTC).
        /// </summary>
        [JsonPropertyName("executedAt")]
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional source identifier — e.g., “VaultQueryHelper”, “VaultQueryAdapter”.
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Optional warning or diagnostic message, if execution succeeded with caveats.
        /// </summary>
        [JsonPropertyName("warning")]
        public string? Warning { get; set; }

        public VaultQueryResult() { }

        public VaultQueryResult(List<Dictionary<string, object>> rows, long elapsedMs, string? queryText = null, string? source = null)
        {
            Rows = rows;
            RowCount = rows?.Count ?? 0;
            ElapsedMs = elapsedMs;
            QueryText = queryText;
            Source = source;
        }
    }
}
