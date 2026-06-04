using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteUsageDto
    {
        public string NoteID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool CanSafelyDelete { get; set; }
        public int UsageCount { get; set; }
        public int VaultHeaderUsageCount { get; set; }
        public int ProjectUsageCount { get; set; }
        public int SessionUsageCount { get; set; }
        public int WorkflowUsageCount { get; set; }
        public int WorkflowNodeUsageCount { get; set; }
        public int WorkflowLineUsageCount { get; set; }
        public int WorkflowLineStepUsageCount { get; set; }
        public int FileRefUsageCount { get; set; }
        public int NoteRelationUsageCount { get; set; }
        public int TagUsageCount { get; set; }
        public List<VaultNoteUsageLocationDto> Locations { get; set; } = new();
    }
}
