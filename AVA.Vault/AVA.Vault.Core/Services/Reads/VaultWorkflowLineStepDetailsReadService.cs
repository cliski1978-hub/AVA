using System;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultWorkflowLineStepDetailsReadService : IVaultWorkflowLineStepDetailsReadService
    {
        private readonly IVaultWorkflowLineStepQueryService _stepQuery;
        private readonly IVaultWorkflowLineStepNotesReadService _stepNotesRead;
        private readonly IVaultWorkflowLineStepFileRefsReadService _stepFilesRead;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineStepDetailsReadService(
            IVaultWorkflowLineStepQueryService stepQuery,
            IVaultWorkflowLineStepNotesReadService stepNotesRead,
            IVaultWorkflowLineStepFileRefsReadService stepFilesRead,
            VaultLogger logger)
        {
            _stepQuery = stepQuery ?? throw new ArgumentNullException(nameof(stepQuery));
            _stepNotesRead = stepNotesRead ?? throw new ArgumentNullException(nameof(stepNotesRead));
            _stepFilesRead = stepFilesRead ?? throw new ArgumentNullException(nameof(stepFilesRead));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineStepDetailsDto?> GetWorkflowLineStepDetailsAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            var step = await _stepQuery.GetByIdAsync(workflowLineStepId, ct);
            if (step == null)
                return null;

            var notesResponse = await _stepNotesRead.GetNotesForWorkflowLineStepAsync(workflowLineStepId, ct);
            var filesResponse = await _stepFilesRead.GetFilesForWorkflowLineStepAsync(workflowLineStepId, ct);

            return new VaultWorkflowLineStepDetailsDto
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
                UpdatedAt = step.UpdatedAt,
                Notes = notesResponse,
                Files = filesResponse
            };
        }

        public async Task<VaultAttachedNotesResponse> GetWorkflowLineStepNotesAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            return await _stepNotesRead.GetNotesForWorkflowLineStepAsync(workflowLineStepId, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetWorkflowLineStepFilesAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            return await _stepFilesRead.GetFilesForWorkflowLineStepAsync(workflowLineStepId, ct);
        }
    }
}
