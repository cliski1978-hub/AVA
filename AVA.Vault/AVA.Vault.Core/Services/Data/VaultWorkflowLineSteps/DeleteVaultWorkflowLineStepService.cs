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
    /// Deletes a VaultWorkflowLineStep and related note-link and file-ref-link records.
    /// Notes and file references are not deleted because they may be shared outside the workflow line step.
    /// </summary>
    public class DeleteVaultWorkflowLineStepService : ApiServiceBase<DeleteVaultWorkflowLineStepRequest, DeleteVaultWorkflowLineStepResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineStepService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineStepResponse DoWork(DeleteVaultWorkflowLineStepRequest request)
        {
            var response = new DeleteVaultWorkflowLineStepResponse();

            try
            {
                var workflowLineStep = Context.Set<VaultWorkflowLineStep>().FirstOrDefault(s => s.ID == request.WorkflowLineStepID);

                if (workflowLineStep == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line step not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => n.WorkflowLineStepID == workflowLineStep.ID).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => f.WorkflowLineStepID == workflowLineStep.ID).ToList();

                foreach (var item in workflowLineStepNotes)
                    Context.Set<VaultWorkflowLineStepNote>().Remove(item);

                foreach (var item in workflowLineStepFileRefs)
                    Context.Set<VaultWorkflowLineStepFileRef>().Remove(item);

                Context.Set<VaultWorkflowLineStep>().Remove(workflowLineStep);
                Context.Flush();

                response.Deleted = true;
                response.UserMessage = "Vault workflow line step and related link records deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineStepService), $"Deleted VaultWorkflowLineStep [{workflowLineStep.ID}] '{workflowLineStep.Name}' and related link records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStep", workflowLineStep.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineStepService), "Error deleting VaultWorkflowLineStep.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line step.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultWorkflowLineStepRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineStepID))
                yield return new ValidationResult("WorkflowLineStepID is required.");
        }
    }

    public class DeleteVaultWorkflowLineStepResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}