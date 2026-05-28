using System.ComponentModel.DataAnnotations;

namespace AVA.Vault.Core.Data.Models
{
    public class ActivityLogEntry
    {
        [Key]
        public int ID { get; set; }

        public DateTime Date { get; set; }

        [StringLength(100)]
        public string UserName { get; set; }

        public int Level { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        public string TargetID { get; set; }

        [StringLength(50)]
        public string Action { get; set; }

        public string Message { get; set; }
    }
}