using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultWorkflowLineDetailsReadService : IVaultWorkflowLineDetailsReadService
    {
        private readonly IVaultWorkflowLineQueryService _lineQuery;
        private readonly IVaultWorkflowLineNotesReadService _lineNotesRead;
        private readonly IVaultWorkflowLineFileRefsReadService _lineFilesRead;
        private readonly IVaultWorkflowLineStepQueryService _stepQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineDetailsReadService(
            IVaultWorkflowLineQueryService lineQuery,
            IVaultWorkflowLineNotesReadService lineNotesRead,
            IVaultWorkflowLineFileRefsReadService lineFilesRead,
            IVaultWorkflowLineStepQueryService stepQuery,
            VaultLogger logger)
        {
            _lineQuery = lineQuery ?? throw new ArgumentNullException(nameof(lineQuery));
            _lineNotesRead = lineNotesRead ?? throw new ArgumentNullException(nameof(lineNotesRead));
            _lineFilesRead = lineFilesRead ?? throw new ArgumentNullException(nameof(lineFilesRead));
            _stepQuery = stepQuery ?? throw new ArgumentNullException(nameof(stepQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineDetailsDto?> GetWorkflowLineDetailsAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var line = await _lineQuery.GetByIdAsync(workflowLineId, ct);
            if (line == null)
                return null;

            var steps = await _stepQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            var notesResponse = await _lineNotesRead.GetNotesForWorkflowLineAsync(workflowLineId, ct);
            var filesResponse = await _lineFilesRead.GetFilesForWorkflowLineAsync(workflowLineId, ct);

            return new VaultWorkflowLineDetailsDto
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
                UpdatedAt = line.UpdatedAt,
                Notes = notesResponse,
                Files = filesResponse,
                Steps = steps
                    .OrderBy(s => s.StepOrder).ThenBy(s => s.Name)
                    .Select(s => new VaultWorkflowLineStepDto
                    {
                        WorkflowLineStepID = s.ID,
                        WorkflowLineID = s.WorkflowLineID,
                        Name = s.Name,
                        Description = s.Description,
                        StepType = s.StepType,
                        Instructions = s.Instructions,
                        IsRequired = s.IsRequired,
                        StepOrder = s.StepOrder,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToList()
            };
        }

        public async Task<VaultAttachedNotesResponse> GetWorkflowLineNotesAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            return await _lineNotesRead.GetNotesForWorkflowLineAsync(workflowLineId, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetWorkflowLineFilesAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            return await _lineFilesRead.GetFilesForWorkflowLineAsync(workflowLineId, ct);
        }

        public async Task<List<VaultWorkflowLineStepDto>> GetWorkflowLineStepsAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var steps = await _stepQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            return steps
                .OrderBy(s => s.StepOrder).ThenBy(s => s.Name)
                .Select(s => new VaultWorkflowLineStepDto
                {
                    WorkflowLineStepID = s.ID,
                    WorkflowLineID = s.WorkflowLineID,
                    Name = s.Name,
                    Description = s.Description,
                    StepType = s.StepType,
                    Instructions = s.Instructions,
                    IsRequired = s.IsRequired,
                    StepOrder = s.StepOrder,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToList();
        }
    }
}
