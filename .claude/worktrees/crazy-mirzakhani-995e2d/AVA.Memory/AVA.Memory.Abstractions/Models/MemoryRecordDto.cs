using System;
using System.Collections.Generic;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a portable memory record used across providers and APIs.
    /// </summary>
    public class MemoryRecordDto
    {
        public string ID { get; set; } = string.Empty;
        public string? Text { get; set; }

        public List<MemoryVectorDto> Vectors { get; set; } = new();
        public List<MemoryTagDto> Tags { get; set; } = new();
        public List<MemoryMetadataDto> Metadata { get; set; } = new();

        public string? EpisodeId { get; set; }
        public string? ContextId { get; set; }

        public double Salience { get; set; }
        public double Novelty { get; set; }
        public double Frequency { get; set; }
        public double DecayRate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        public string? Source { get; set; }

        public List<AssociationEdgeDto> OutgoingEdges { get; set; } = new();
        public List<AssociationEdgeDto> IncomingEdges { get; set; } = new();

        // ─────────────── Identity Fields ───────────────

        /// <summary>
        /// Primary identity ID associated with this memory record (unique identifier).
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
