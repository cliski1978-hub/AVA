using System;
using System.Text.Json.Serialization;

namespace AVA.Vault.Core.Models.Query
{
    /// <summary>
    /// Represents a standardized error or failure result from a Vault SQL query execution.
    /// Used for both adapter and diagnostic reporting.
    /// </summary>
    public sealed class VaultQueryError
    {
        /// <summary>
        /// Human-readable error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional exception type or category (e.g. SqlException, ValidationError).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Optional SQL text or fragment that caused the error.
        /// </summary>
        [JsonPropertyName("queryText")]
        public string? QueryText { get; set; }

        /// <summary>
        /// UTC timestamp when the error occurred.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional trace or stack information (only in debug mode).
        /// </summary>
        [JsonPropertyName("trace")]
        public string? Trace { get; set; }

        public VaultQueryError() { }

        public VaultQueryError(string message, string type, string? queryText = null, Exception? ex = null)
        {
            Message = message;
            Type = type;
            QueryText = queryText;
            Trace = ex?.ToString();
        }

        public override string ToString() => $"{Type}: {Message}";
    }
}
