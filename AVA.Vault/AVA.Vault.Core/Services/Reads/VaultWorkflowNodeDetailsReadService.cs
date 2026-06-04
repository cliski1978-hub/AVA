using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultWorkflowNodeDetailsReadService : IVaultWorkflowNodeDetailsReadService
    {
        private readonly IVaultWorkflowNodeQueryService _nodeQuery;
        private readonly IVaultWorkflowNodeNotesReadService _nodeNotesRead;
        private readonly IVaultWorkflowNodeFileRefsReadService _nodeFilesRead;
        private readonly IVaultWorkflowLineQueryService _lineQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowNodeDetailsReadService(
            IVaultWorkflowNodeQueryService nodeQuery,
            IVaultWorkflowNodeNotesReadService nodeNotesRead,
            IVaultWorkflowNodeFileRefsReadService nodeFilesRead,
            IVaultWorkflowLineQueryService lineQuery,
            VaultLogger logger)
        {
            _nodeQuery = nodeQuery ?? throw new ArgumentNullException(nameof(nodeQuery));
            _nodeNotesRead = nodeNotesRead ?? throw new ArgumentNullException(nameof(nodeNotesRead));
            _nodeFilesRead = nodeFilesRead ?? throw new ArgumentNullException(nameof(nodeFilesRead));
            _lineQuery = lineQuery ?? throw new ArgumentNullException(nameof(lineQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowNodeDetailsDto?> GetWorkflowNodeDetailsAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var node = await _nodeQuery.GetByIdAsync(workflowNodeId, ct);
            if (node == null)
                return null;

            var incomingLines = await _lineQuery.GetIncomingLinesAsync(workflowNodeId, ct);
            var outgoingLines = await _lineQuery.GetOutgoingLinesAsync(workflowNodeId, ct);
            var notesResponse = await _nodeNotesRead.GetNotesForWorkflowNodeAsync(workflowNodeId, ct);
            var filesResponse = await _nodeFilesRead.GetFilesForWorkflowNodeAsync(workflowNodeId, ct);

            return new VaultWorkflowNodeDetailsDto
            {
                WorkflowNodeID = node.ID,
                WorkflowID = node.WorkflowID,
                Name = node.Name,
                Description = node.Description,
                NodeType = node.NodeType,
                Status = node.Status,
                Instructions = node.Instructions,
                MetadataJson = node.MetadataJson,
                NodeOrder = node.NodeOrder,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt,
                Notes = notesResponse,
                Files = filesResponse,
                IncomingLines = incomingLines
                    .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                    .Select(l => MapLineDto(l))
                    .ToList(),
                OutgoingLines = outgoingLines
                    .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                    .Select(l => MapLineDto(l))
                    .ToList()
            };
        }

        public async Task<VaultAttachedNotesResponse> GetWorkflowNodeNotesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            return await _nodeNotesRead.GetNotesForWorkflowNodeAsync(workflowNodeId, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetWorkflowNodeFilesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            return await _nodeFilesRead.GetFilesForWorkflowNodeAsync(workflowNodeId, ct);
        }

        public async Task<List<VaultWorkflowLineDto>> GetWorkflowNodeIncomingLinesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var lines = await _lineQuery.GetIncomingLinesAsync(workflowNodeId, ct);
            return lines
                .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                .Select(MapLineDto)
                .ToList();
        }

        public async Task<List<VaultWorkflowLineDto>> GetWorkflowNodeOutgoingLinesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var lines = await _lineQuery.GetOutgoingLinesAsync(workflowNodeId, ct);
            return lines
                .OrderBy(l => l.LineOrder).ThenBy(l => l.Name)
                .Select(MapLineDto)
                .ToList();
        }

        private static VaultWorkflowLineDto MapLineDto(VaultWorkflowLine line)
        {
            return new VaultWorkflowLineDto
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
