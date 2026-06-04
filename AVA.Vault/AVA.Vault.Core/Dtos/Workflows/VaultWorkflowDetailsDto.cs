using System;
using System.Collections.Generic;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowDetailsDto
    {
        public string WorkflowID { get; set; } = string.Empty;
        public string ProjectID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string WorkflowType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int NodeCount { get; set; }
        public int LineCount { get; set; }
        public int StepCount { get; set; }
        public int DirectNoteCount { get; set; }
        public int DirectFileCount { get; set; }
        public List<VaultWorkflowNodeDto> Nodes { get; set; } = new();
        public List<VaultWorkflowLineDto> Lines { get; set; } = new();
        public List<VaultWorkflowLineStepDto> Steps { get; set; } = new();
        public VaultAttachedNotesResponse Notes { get; set; } = null!;
        public VaultAttachedFilesResponse Files { get; set; } = null!;
    }
}
