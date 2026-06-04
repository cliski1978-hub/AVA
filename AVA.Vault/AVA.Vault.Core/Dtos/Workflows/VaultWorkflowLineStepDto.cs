using System;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowLineStepDto
    {
        public string WorkflowLineStepID { get; set; } = string.Empty;
        public string WorkflowLineID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string StepType { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public bool IsRequired { get; set; }
        public int StepOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
