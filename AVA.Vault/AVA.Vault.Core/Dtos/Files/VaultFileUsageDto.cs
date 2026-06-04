using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Files
{
    public class VaultFileUsageDto
    {
        public string FileRefID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool CanSafelyDelete { get; set; }
        public int UsageCount { get; set; }
        public int VaultHeaderUsageCount { get; set; }
        public int ProjectUsageCount { get; set; }
        public int SessionUsageCount { get; set; }
        public int NoteUsageCount { get; set; }
        public int FileRefNoteUsageCount { get; set; }
        public int WorkflowUsageCount { get; set; }
        public int WorkflowNodeUsageCount { get; set; }
        public int WorkflowLineUsageCount { get; set; }
        public int WorkflowLineStepUsageCount { get; set; }
        public int FileRelationUsageCount { get; set; }
        public List<VaultFileUsageLocationDto> Locations { get; set; } = new();
    }
}
