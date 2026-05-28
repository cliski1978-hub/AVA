using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AVA.Vault.Core.Models.Query
{
    /// <summary>
    /// Represents a SQL query request passed to VaultQueryAdapter or Query Services.
    /// Used to standardize raw query execution requests.
    /// </summary>
    public sealed class VaultQueryRequest
    {
        /// <summary>
        /// The SQL text or query statement to be executed.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional key/value parameter pairs to bind into the query.
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Optional row limit for result sets (default 500).
        /// </summary>
        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 500;

        /// <summary>
        /// Optional execution timeout in seconds.
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Optional source or origin identifier (e.g. agent name, ServiceHost).
        /// </summary>
        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        /// <summary>
        /// Whether the query is allowed to include joins or advanced SQL clauses.
        /// </summary>
        [JsonPropertyName("allowAdvanced")]
        public bool AllowAdvanced { get; set; } = false;

        /// <summary>
        /// Indicates whether the query text has been pre-validated externally.
        /// </summary>
        [JsonPropertyName("prevalidated")]
        public bool Prevalidated { get; set; } = false;
    }
}
