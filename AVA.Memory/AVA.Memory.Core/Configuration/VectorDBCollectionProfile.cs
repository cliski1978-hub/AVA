using System;
using System.Collections.Generic;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Defines configuration, policies, and runtime metadata
    /// for a single VectorDB collection domain.
    /// </summary>
    public sealed class VectorDBCollectionProfile
    {
        #region Identity and Basic Info

        /// <summary>
        /// Logical name of the collection (e.g., "automation_memory").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description or semantic domain label.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Vector dimensionality of the collection.
        /// </summary>
        public int Dimension { get; set; }

        /// <summary>
        /// Distance metric used by this collection ("cosine", "dot", "euclidean").
        /// </summary>
        public string Metric { get; set; } = "cosine";

        /// <summary>
        /// Indicates whether the collection is active for insertion and query operations.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date/time when the collection profile was last synchronized or updated.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Behavioral Policies

        /// <summary>
        /// Minimum salience score required to retain a record in this collection.
        /// Records below this threshold are subject to pruning.
        /// </summary>
        public double MinSalienceThreshold { get; set; } = 0.25;

        /// <summary>
        /// Age in days after which stale vectors are eligible for archival or deletion.
        /// </summary>
        public int MaxAgeDays { get; set; } = 30;

        /// <summary>
        /// Defines how aggressively old records decay in priority (0 = none, 1 = very fast).
        /// </summary>
        public double DecayRate { get; set; } = 0.05;

        /// <summary>
        /// Whether to auto-create this collection if referenced by router but not found.
        /// </summary>
        public bool AllowAutoCreate { get; set; } = true;

        /// <summary>
        /// Whether the maintenance system can rebalance vectors between this and sibling collections.
        /// </summary>
        public bool AllowRebalancing { get; set; } = true;

        #endregion

        #region Analytics and Metrics

        /// <summary>
        /// Approximate number of vectors currently stored.
        /// </summary>
        public int EstimatedVectorCount { get; set; }

        /// <summary>
        /// Average similarity distance observed during searches.
        /// Useful for tracking embedding drift.
        /// </summary>
        public double AverageSimilarity { get; set; }

        /// <summary>
        /// Cumulative number of upserts or updates since collection creation.
        /// </summary>
        public long TotalUpserts { get; set; }

        /// <summary>
        /// Cumulative number of queries served.
        /// </summary>
        public long TotalQueries { get; set; }

        #endregion

        #region Metadata

        /// <summary>
        /// Arbitrary metadata map for additional runtime properties or annotations.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns a readable diagnostic summary of this profile.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} | {Metric} | Dim={Dimension} | Active={IsActive} | Vectors≈{EstimatedVectorCount}";
        }

        #endregion
    }
}
