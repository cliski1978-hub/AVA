using System;
using System.Collections.Generic;
using System.Linq;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a single vector component or an aggregated vector
    /// for a memory record. Designed to bridge the SQL persistence
    /// model (row-per-value) with the VectorDB runtime model (array-per-vector).
    /// </summary>
    public class MemoryVectorDto
    {
        #region Scalar Fields (for per-value SQL mapping)

        /// <summary>
        /// Database or row identifier (if persisted).
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Foreign key to the parent MemoryRecord.
        /// </summary>
        public string RecordID { get; set; } = string.Empty;

        /// <summary>
        /// Index position of this value in the embedding.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Single float component of the vector.
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Timestamp of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp of last modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Identity Fields (from IdentityLinkedEntityBase)

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

        #endregion

        #region Aggregated View (for vector operations)

        /// <summary>
        /// Flattened array of vector components, used by VectorDB drivers and tests.
        /// This property is not persisted directly; it is populated or derived at runtime.
        /// </summary>
        public float[] Values
        {
            get
            {
                if (_components?.Count > 0)
                    return _components.OrderBy(v => v.Index).Select(v => v.Value).ToArray();
                return Array.Empty<float>();
            }
            set
            {
                _components = value?
                    .Select((val, i) => new MemoryVectorDto
                    {
                        RecordID = RecordID,
                        Index = i,
                        Value = val,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToList();
            }
        }

        private List<MemoryVectorDto>? _components;

        #endregion
    }
}
