using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Deletes a VaultWorkflowLine and related workflow line step, note-link, and file-ref-link records.
    /// Notes and file references are not deleted because they may be shared outside the workflow line.
    /// </summary>
    public class DeleteVaultWorkflowLineService : ApiServiceBase<DeleteVaultWorkflowLineRequest, DeleteVaultWorkflowLineResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineResponse DoWork(DeleteVaultWorkflowLineRequest request)
        {
            var response = new DeleteVaultWorkflowLineResponse();

            try
            {
                var workflowLine = Context.Set<VaultWorkflowLine>().FirstOrDefault(l => l.ID == request.WorkflowLineID);

                if (workflowLine == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineSteps = Context.Set<VaultWorkflowLineStep>().Where(s => s.WorkflowLineID == workflowLine.ID).ToList();
                var workflowLineStepIds = workflowLineSteps.Select(s => s.ID).ToList();

                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => workflowLineStepIds.Contains(n.WorkflowLineStepID)).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => workflowLineStepIds.Contains(f.WorkflowLineStepID)).ToList();

                var workflowLineNotes = Context.Set<VaultWorkflowLineNote>().Where(n => n.WorkflowLineID == workflowLine.ID).ToList();
                var workflowLineFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => f.WorkflowLineID == workflowLine.ID).ToList();

                foreach (var item in workflowLineStepNotes)
                    Context.Set<VaultWorkflowLineStepNote>().Remove(item);

                foreach (var item in workflowLineStepFileRefs)
                    Context.Set<VaultWorkflowLineStepFileRef>().Remove(item);

                foreach (var item in workflowLineSteps)
                    Context.Set<VaultWorkflowLineStep>().Remove(item);

                foreach (var item in workflowLineNotes)
                    Context.Set<VaultWorkflowLineNote>().Remove(item);

                foreach (var item in workflowLineFileRefs)
                    Context.Set<VaultWorkflowLineFileRef>().Remove(item);

                Context.Set<VaultWorkflowLine>().Remove(workflowLine);
                Context.Flush();

                response.Deleted = true;
                response.UserMessage = "Vault workflow line and related workflow line data deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineService), $"Deleted VaultWorkflowLine [{workflowLine.ID}] '{workflowLine.Name}' and related records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLine", workflowLine.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineService), "Error deleting VaultWorkflowLine.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultWorkflowLineRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineID))
                yield return new ValidationResult("WorkflowLineID is required.");
        }
    }

    public class DeleteVaultWorkflowLineResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}