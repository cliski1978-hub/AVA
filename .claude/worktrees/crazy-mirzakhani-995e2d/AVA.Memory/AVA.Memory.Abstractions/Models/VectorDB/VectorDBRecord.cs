using System;
using System.Collections.Generic;

namespace AVA.Memory.Abstractions.Models.VectorDB
{
    /// <summary>
    /// Represents a single stored embedding and its metadata within a vector collection.
    /// </summary>
    [Serializable]
    public sealed class VectorDBRecord
    {
        #region Core Identity

        /// <summary>
        /// Unique identifier of this record.
        /// Should be globally unique (UUID, GUID, or deterministic hash).
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Logical name of the collection or namespace this record belongs to.
        /// </summary>
        public string Collection { get; set; } = string.Empty;

        /// <summary>
        /// Optional short description or semantic label for this record.
        /// </summary>
        public string? Description { get; set; }

        #endregion

        #region Vector Data

        /// <summary>
        /// Embedding vector values used for similarity search.
        /// Typically a dense array of floats representing the semantic encoding.
        /// </summary>
        public float[] Vector { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Arbitrary metadata or contextual payload associated with this vector.
        /// Includes attributes such as source, category, relevance score, etc.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Tags used for filtering and semantic grouping.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        #endregion

        #region Temporal Metadata

        /// <summary>
        /// Timestamp for record creation.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp for last modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors

        public VectorDBRecord() { }

        public VectorDBRecord(
            string id,
            float[] vector,
            string collection = "",
            string? description = null,
            Dictionary<string, object>? metadata = null,
            string[]? tags = null)
        {
            Id = id;
            Vector = vector ?? Array.Empty<float>();
            Collection = collection ?? string.Empty;
            Description = description;
            Metadata = metadata ?? new Dictionary<string, object>();
            Tags = tags ?? Array.Empty<string>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            var tagStr = Tags != null && Tags.Length > 0 ? string.Join(",", Tags) : "none";
            var coll = string.IsNullOrWhiteSpace(Collection) ? "default" : Collection;
            return $"{Id} | Coll={coll} | Dim={Vector?.Length ?? 0} | Tags=[{tagStr}]";
        }

        #endregion
    }
}
