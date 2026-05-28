using System;
using System.Collections.Generic;

namespace AVA.Memory.Abstractions.Models.VectorDB
{
    /// <summary>
    /// Represents the result of a similarity search against a vector collection.
    /// Includes score, metadata, optional vector, and collection context.
    /// </summary>
    [Serializable]
    public sealed class VectorDbSearchResult
    {
        #region Properties

        /// <summary>
        /// Unique identifier of the matched record.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Similarity score for this match (higher = closer, depending on distance metric).
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Optional vector embedding (if returned by backend).
        /// </summary>
        public float[]? Vector { get; set; }

        /// <summary>
        /// Metadata or contextual payload returned with this result.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; } = new();

        /// <summary>
        /// Tags or semantic labels associated with this record.
        /// </summary>
        public string[]? Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Collection name from which the result was retrieved.
        /// </summary>
        public string Collection { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the record was created in the source collection.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in the source collection.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors

        public VectorDbSearchResult() { }

        public VectorDbSearchResult(
            string id,
            float score,
            Dictionary<string, object>? metadata = null,
            float[]? vector = null,
            string? collection = null,
            string[]? tags = null)
        {
            Id = id;
            Score = score;
            Metadata = metadata ?? new Dictionary<string, object>();
            Vector = vector ?? Array.Empty<float>();
            Collection = collection ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            var tagList = Tags != null && Tags.Length > 0 ? string.Join(",", Tags) : "none";
            return $"{Id} (score={Score:0.###}, coll={Collection}, tags=[{tagList}])";
        }

        #endregion
    }
}
