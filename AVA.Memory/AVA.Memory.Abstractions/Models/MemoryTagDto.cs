using System;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a tag or label attached to a memory record.
    /// </summary>
    public class MemoryTagDto
    {
        public int ID { get; set; }
        public string RecordID { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

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
