using System;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowGraphLineDto
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
    }
}
