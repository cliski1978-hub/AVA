using System;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowSummaryDto
    {
        public string WorkflowID { get; set; } = string.Empty;
        public string ProjectID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string WorkflowType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int NodeCount { get; set; }
        public int LineCount { get; set; }
        public int StepCount { get; set; }
        public int DirectNoteCount { get; set; }
        public int DirectFileCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
