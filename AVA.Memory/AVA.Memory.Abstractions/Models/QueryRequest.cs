using System;
using System.Collections.Generic;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Defines a memory query request, including raw text or vector embedding,
    /// along with filters, thresholds, and result parameters.
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// Raw query text. Used to generate an embedding if Embedding is not provided.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional embedding vector for semantic search.
        /// </summary>
        public float[]? Embedding { get; set; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Minimum similarity score threshold.
        /// </summary>
        public double MinScore { get; set; } = 0.0;

        /// <summary>
        /// Optional tag filters for narrowing search scope.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Optional metadata filters (key/value pairs).
        /// </summary>
        public Dictionary<string, object> MetadataFilters { get; set; } = new();
    }
}
