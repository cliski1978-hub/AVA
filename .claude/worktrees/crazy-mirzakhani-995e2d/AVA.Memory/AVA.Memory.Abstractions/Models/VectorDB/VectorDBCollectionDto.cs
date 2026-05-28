using System;
using System.Collections.Generic;

namespace AVA.Memory.Abstractions.Models.VectorDB
{
    /// <summary>
    /// Data Transfer Object representing a VectorDB collection.
    /// Used for external communication (e.g., API responses, telemetry, registry sync)
    /// without exposing internal VectorCollection models directly.
    /// </summary>
    public class VectorDBCollectionDto
    {
        /// <summary>
        /// The unique name of the collection (e.g., "automation_memory").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional short description of the collection.
        /// Used for UI display, logs, and monitoring.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The logical grouping or namespace this collection belongs to.
        /// For example, "core_memory", "project_alpha", or "runtime_cache".
        /// </summary>
        public string? Collection { get; set; }

        /// <summary>
        /// Indicates whether the collection is initialized and available in the backend.
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// The number of vectors currently stored in this collection.
        /// </summary>
        public int VectorCount { get; set; }

        /// <summary>
        /// The dimensionality of vectors in this collection.
        /// Typically matches the embedding model’s output size (e.g., 1536).
        /// </summary>
        public int Dimension { get; set; }

        /// <summary>
        /// The internal distance metric name used for similarity scoring
        /// (e.g., "Cosine", "Dot", "Euclidean").
        /// This value matches the backend driver’s expected keyword.
        /// </summary>
        public string DistanceMetric { get; set; } = "Cosine";

        /// <summary>
        /// The normalized metric label used internally across AVA
        /// (e.g., "cosine", "dot", "euclidean").
        /// </summary>
        public string Metric { get; set; } = "cosine";

        /// <summary>
        /// Timestamp when this collection was first created in the backend.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp of the most recent update or metadata sync.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional centroid vector representing the mean embedding of the collection.
        /// May be null if the driver does not compute or expose centroids.
        /// </summary>
        public float[]? Centroid { get; set; }

        /// <summary>
        /// Optional metadata map for backend-specific or semantic attributes.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Returns a short diagnostic string describing the collection.
        /// </summary>
        public override string ToString()
        {
            var desc = string.IsNullOrWhiteSpace(Description) ? "No description" : Description;
            return $"{Name} ({Collection ?? "default"}) | Count={VectorCount} | Dim={Dimension} | Metric={Metric} | {desc}";
        }
    }
}
