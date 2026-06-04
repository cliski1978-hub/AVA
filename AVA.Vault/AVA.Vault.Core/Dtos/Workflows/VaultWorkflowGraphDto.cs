using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowGraphDto
    {
        public string WorkflowID { get; set; } = string.Empty;
        public List<VaultWorkflowGraphNodeDto> Nodes { get; set; } = new();
        public List<VaultWorkflowGraphLineDto> Lines { get; set; } = new();
    }
}
