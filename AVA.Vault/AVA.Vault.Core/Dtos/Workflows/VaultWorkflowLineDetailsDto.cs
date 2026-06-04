using System;
using System.Collections.Generic;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowLineDetailsDto
    {
        public string WorkflowLineID { get; set; } = string.Empty;
        public string WorkflowID { get; set; } = string.Empty;
        public string SourceWorkflowNodeID { get; set; } = string.Empty;
        public string TargetWorkflowNodeID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string LineType { get; set; } = string.Empty;
        public bool IsDefaultLine { get; set; }
        public int LineOrder { get; set; }
        public string? ConditionJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public VaultAttachedNotesResponse Notes { get; set; } = null!;
        public VaultAttachedFilesResponse Files { get; set; } = null!;
        public List<VaultWorkflowLineStepDto> Steps { get; set; } = new();
    }
}
