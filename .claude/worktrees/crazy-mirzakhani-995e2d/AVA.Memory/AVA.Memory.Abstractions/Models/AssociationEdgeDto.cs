using System;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a directed relationship (association) between two memory records.
    /// </summary>
    public class AssociationEdgeDto
    {
        /// <summary>
        /// Unique identifier for this edge.
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Source memory record ID.
        /// </summary>
        public string FromID { get; set; } = string.Empty;

        /// <summary>
        /// Target memory record ID.
        /// </summary>
        public string ToID { get; set; } = string.Empty;

        /// <summary>
        /// Type or category of the relationship (e.g., "causes", "references", etc.).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Strength or weight of the association (normalized 0–1).
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Timestamp of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of last modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // ─────────────── Identity Fields ───────────────

        /// <summary>
        /// Primary identity ID associated with this entity (unique identifier).
        /// </summary>
        public string PrimaryIdentityId { get; set; } = string.Empty;

        /// <summary>
        /// Primary identity handle (human-readable short handle).
        /// </summary>
        public string PrimaryIdentityHandle { get; set; } = string.Empty;

        /// <summary>
        /// Type of the primary identity (e.g., "human", "agent", "system").
        /// </summary>
        public string PrimaryIdentityType { get; set; } = "unknown";

        /// <summary>
        /// Serialized list of associated identities in binary form.
        /// </summary>
        public byte[]? IdentityList { get; set; }
    }
}
