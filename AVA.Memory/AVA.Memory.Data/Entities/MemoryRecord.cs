using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Memory.Data.Entities
{
    [Table("MemoryRecords")]
    public class MemoryRecord : IdentityLinkedEntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [Column(TypeName = "nvarchar(max)")]
        public string? Text { get; set; }

        // --- Normalized Collections ---
        public ICollection<MemoryVector> Vectors { get; set; } = new List<MemoryVector>();
        public ICollection<MemoryTag> Tags { get; set; } = new List<MemoryTag>();
        public ICollection<MemoryMetadata> Metadata { get; set; } = new List<MemoryMetadata>();

        // --- Context / Episode ---
        public string? EpisodeId { get; set; }
        public string? ContextId { get; set; }

        // --- Metrics ---
        public double Salience { get; set; }
        public double Novelty { get; set; }
        public double Frequency { get; set; }
        public double DecayRate { get; set; }

        // --- Timestamps ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAccessedAt { get; set; }

        public string? Source { get; set; }

        // --- Graph Relationships ---
        [InverseProperty(nameof(AssociationEdge.FromRecord))]
        public ICollection<AssociationEdge> OutgoingEdges { get; set; } = new List<AssociationEdge>();

        [InverseProperty(nameof(AssociationEdge.ToRecord))]
        public ICollection<AssociationEdge> IncomingEdges { get; set; } = new List<AssociationEdge>();
    }
}
