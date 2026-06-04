using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultWorkflowGraphReadService : IVaultWorkflowGraphReadService
    {
        private readonly IVaultWorkflowNodeQueryService _nodeQuery;
        private readonly IVaultWorkflowLineQueryService _lineQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowGraphReadService(
            IVaultWorkflowNodeQueryService nodeQuery,
            IVaultWorkflowLineQueryService lineQuery,
            VaultLogger logger)
        {
            _nodeQuery = nodeQuery ?? throw new ArgumentNullException(nameof(nodeQuery));
            _lineQuery = lineQuery ?? throw new ArgumentNullException(nameof(lineQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowGraphDto?> GetWorkflowGraphAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var nodes = await _nodeQuery.GetByWorkflowIdAsync(workflowId, ct);
            var lines = await _lineQuery.GetByWorkflowIdAsync(workflowId, ct);

            return new VaultWorkflowGraphDto
            {
                WorkflowID = workflowId,
                Nodes = nodes
                    .OrderBy(n => n.NodeOrder).ThenBy(n => n.Name)
                    .Select(MapGraphNodeDto)
                    .ToList(),
                Lines = lines
                    .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                    .Select(MapGraphLineDto)
                    .ToList()
            };
        }

        public async Task<VaultWorkflowGraphNodeDto?> GetNodeGraphContextAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var node = await _nodeQuery.GetByIdAsync(workflowNodeId, ct);
            if (node == null)
                return null;

            return MapGraphNodeDto(node);
        }

        public async Task<List<VaultWorkflowGraphLineDto>> GetOutgoingNodeLinksAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var lines = await _lineQuery.GetOutgoingLinesAsync(workflowNodeId, ct);
            return lines
                .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                .Select(MapGraphLineDto)
                .ToList();
        }

        public async Task<List<VaultWorkflowGraphLineDto>> GetIncomingNodeLinksAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var lines = await _lineQuery.GetIncomingLinesAsync(workflowNodeId, ct);
            return lines
                .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                .Select(MapGraphLineDto)
                .ToList();
        }

        private static VaultWorkflowGraphNodeDto MapGraphNodeDto(VaultWorkflowNode node)
        {
            return new VaultWorkflowGraphNodeDto
            {
                WorkflowNodeID = node.ID,
                WorkflowID = node.WorkflowID,
                Name = node.Name,
                Description = node.Description,
                NodeType = node.NodeType,
                Status = node.Status,
                NodeOrder = node.NodeOrder,
                MetadataJson = node.MetadataJson,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt
            };
        }

        private static VaultWorkflowGraphLineDto MapGraphLineDto(VaultWorkflowLine line)
        {
            return new VaultWorkflowGraphLineDto
            {
                WorkflowLineID = line.ID,
                WorkflowID = line.WorkflowID,
                SourceWorkflowNodeID = line.SourceWorkflowNodeID,
                TargetWorkflowNodeID = line.TargetWorkflowNodeID,
                Name = line.Name,
                Description = line.Description,
                LineType = line.LineType,
                IsDefaultLine = line.IsDefaultLine,
                LineOrder = line.LineOrder,
                ConditionJson = line.ConditionJson,
                CreatedAt = line.CreatedAt,
                UpdatedAt = line.UpdatedAt
            };
        }
    }
}
