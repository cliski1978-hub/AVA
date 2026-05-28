using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Memory.Data.Entities
{
    [Table("MemoryTags")]
    public class MemoryTag : IdentityLinkedEntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public string RecordID { get; set; } = default!;

        [ForeignKey(nameof(RecordID))]
        public MemoryRecord? Record { get; set; }

        [Required]
        public string Tag { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
