using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Memory.Data.Entities
{
    [Table("AssociationEdges")]
    public class AssociationEdge : IdentityLinkedEntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string FromRecordID { get; set; } = default!;

        [Required]
        public string ToRecordID { get; set; } = default!;

        [Required]
        public string Type { get; set; } = string.Empty;

        public double Weight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(FromRecordID))]
        public MemoryRecord? FromRecord { get; set; }

        [ForeignKey(nameof(ToRecordID))]
        public MemoryRecord? ToRecord { get; set; }
    }
}
