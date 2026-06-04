using System;
using System.Collections.Generic;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Dtos.Workflows
{
    public class VaultWorkflowNodeDetailsDto
    {
        public string WorkflowNodeID { get; set; } = string.Empty;
        public string WorkflowID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string NodeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public string? MetadataJson { get; set; }
        public int NodeOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public VaultAttachedNotesResponse Notes { get; set; } = null!;
        public VaultAttachedFilesResponse Files { get; set; } = null!;
        public List<VaultWorkflowLineDto> IncomingLines { get; set; } = new();
        public List<VaultWorkflowLineDto> OutgoingLines { get; set; } = new();
    }
}
