using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultWorkflowDetailsReadService : IVaultWorkflowDetailsReadService
    {
        private readonly IVaultWorkflowQueryService _workflowQuery;
        private readonly IVaultWorkflowNodeQueryService _nodeQuery;
        private readonly IVaultWorkflowLineQueryService _lineQuery;
        private readonly IVaultWorkflowLineStepQueryService _stepQuery;
        private readonly IVaultWorkflowNotesReadService _workflowNotesRead;
        private readonly IVaultWorkflowFileRefsReadService _workflowFilesRead;
        private readonly VaultLogger _logger;

        public VaultWorkflowDetailsReadService(
            IVaultWorkflowQueryService workflowQuery,
            IVaultWorkflowNodeQueryService nodeQuery,
            IVaultWorkflowLineQueryService lineQuery,
            IVaultWorkflowLineStepQueryService stepQuery,
            IVaultWorkflowNotesReadService workflowNotesRead,
            IVaultWorkflowFileRefsReadService workflowFilesRead,
            VaultLogger logger)
        {
            _workflowQuery = workflowQuery ?? throw new ArgumentNullException(nameof(workflowQuery));
            _nodeQuery = nodeQuery ?? throw new ArgumentNullException(nameof(nodeQuery));
            _lineQuery = lineQuery ?? throw new ArgumentNullException(nameof(lineQuery));
            _stepQuery = stepQuery ?? throw new ArgumentNullException(nameof(stepQuery));
            _workflowNotesRead = workflowNotesRead ?? throw new ArgumentNullException(nameof(workflowNotesRead));
            _workflowFilesRead = workflowFilesRead ?? throw new ArgumentNullException(nameof(workflowFilesRead));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowDetailsDto?> GetWorkflowDetailsAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var workflow = await _workflowQuery.GetByIdAsync(workflowId, ct);
            if (workflow == null)
                return null;

            var nodes = await _nodeQuery.GetByWorkflowIdAsync(workflowId, ct);
            var lines = await _lineQuery.GetByWorkflowIdAsync(workflowId, ct);
            var orderedNodes = nodes.OrderBy(n => n.NodeOrder).ThenBy(n => n.Name).ToList();
            var orderedLines = lines.OrderBy(l => l.LineOrder).ThenBy(l => l.Name).ToList();

            var allSteps = await Task.WhenAll(orderedLines.Select(l =>
                _stepQuery.GetByWorkflowLineIdAsync(l.ID, ct)));
            var orderedSteps = allSteps.SelectMany(s => s)
                .OrderBy(s => s.StepOrder).ThenBy(s => s.Name).ToList();

            var notesResponse = await _workflowNotesRead.GetNotesForWorkflowAsync(workflowId, ct);
            var filesResponse = await _workflowFilesRead.GetFilesForWorkflowAsync(workflowId, ct);

            return new VaultWorkflowDetailsDto
            {
                WorkflowID = workflow.ID,
                ProjectID = workflow.ProjectID,
                Name = workflow.Name,
                Description = workflow.Description,
                WorkflowType = workflow.WorkflowType,
                Status = workflow.Status,
                SortOrder = workflow.SortOrder,
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt,
                NodeCount = orderedNodes.Count,
                LineCount = orderedLines.Count,
                StepCount = orderedSteps.Count,
                DirectNoteCount = notesResponse.TotalCount,
                DirectFileCount = filesResponse.TotalCount,
                Nodes = orderedNodes.Select(n => MapNodeDto(n)).ToList(),
                Lines = orderedLines.Select(l => MapLineDto(l)).ToList(),
                Steps = orderedSteps.Select(s => MapStepDto(s)).ToList(),
                Notes = notesResponse,
                Files = filesResponse
            };
        }

        public async Task<VaultWorkflowSummaryDto?> GetWorkflowSummaryAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var workflow = await _workflowQuery.GetByIdAsync(workflowId, ct);
            if (workflow == null)
                return null;

            var nodeCount = await _nodeQuery.CountByWorkflowIdAsync(workflowId, ct);
            var lineCount = await _lineQuery.CountByWorkflowIdAsync(workflowId, ct);

            var lines = await _lineQuery.GetByWorkflowIdAsync(workflowId, ct);
            var stepCount = lines.Count > 0
                ? (await Task.WhenAll(lines.Select(l =>
                    _stepQuery.CountByWorkflowLineIdAsync(l.ID, ct)))).Sum()
                : 0;

            var notesResponse = await _workflowNotesRead.GetNotesForWorkflowAsync(workflowId, ct);
            var filesResponse = await _workflowFilesRead.GetFilesForWorkflowAsync(workflowId, ct);

            return new VaultWorkflowSummaryDto
            {
                WorkflowID = workflow.ID,
                ProjectID = workflow.ProjectID,
                Name = workflow.Name,
                Description = workflow.Description,
                WorkflowType = workflow.WorkflowType,
                Status = workflow.Status,
                NodeCount = nodeCount,
                LineCount = lineCount,
                StepCount = stepCount,
                DirectNoteCount = notesResponse.TotalCount,
                DirectFileCount = filesResponse.TotalCount,
                UpdatedAt = workflow.UpdatedAt
            };
        }

        private static VaultWorkflowNodeDto MapNodeDto(VaultWorkflowNode node)
        {
            return new VaultWorkflowNodeDto
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
                UpdatedAt = node.UpdatedAt
            };
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

        private static VaultWorkflowLineStepDto MapStepDto(VaultWorkflowLineStep step)
        {
            return new VaultWorkflowLineStepDto
            {
                WorkflowLineStepID = step.ID,
                WorkflowLineID = step.WorkflowLineID,
                Name = step.Name,
                Description = step.Description,
                StepType = step.StepType,
                Instructions = step.Instructions,
                IsRequired = step.IsRequired,
                StepOrder = step.StepOrder,
                CreatedAt = step.CreatedAt,
                UpdatedAt = step.UpdatedAt
            };
        }
    }
}
