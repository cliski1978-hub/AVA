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
    /// Deletes a VaultWorkflowLineStepFileRef link between a VaultWorkflowLineStep and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowLineStepFileRefService : ApiServiceBase<DeleteVaultWorkflowLineStepFileRefRequest, DeleteVaultWorkflowLineStepFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineStepFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineStepFileRefResponse DoWork(DeleteVaultWorkflowLineStepFileRefRequest request)
        {
            var response = new DeleteVaultWorkflowLineStepFileRefResponse();

            try
            {
                var workflowLineStepFileRef = Context.Set<VaultWorkflowLineStepFileRef>().FirstOrDefault(f => f.ID == request.WorkflowLineStepFileRefID);

                if (workflowLineStepFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line step file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineStepId = workflowLineStepFileRef.WorkflowLineStepID;
                var fileRefId = workflowLineStepFileRef.FileRefID;

                Context.Set<VaultWorkflowLineStepFileRef>().Remove(workflowLineStepFileRef);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowLineStepFileRefID = request.WorkflowLineStepFileRefID;
                response.WorkflowLineStepID = workflowLineStepId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault workflow line step file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineStepFileRefService), $"Deleted VaultWorkflowLineStepFileRef [{request.WorkflowLineStepFileRefID}] WorkflowLineStep [{workflowLineStepId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepFileRef", request.WorkflowLineStepFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineStepFileRefService), "Error deleting VaultWorkflowLineStepFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line step file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowLineStepFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineStepFileRefID))
                yield return new ValidationResult("WorkflowLineStepFileRefID is required.");
        }
    }

    public class DeleteVaultWorkflowLineStepFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowLineStepFileRefID { get; set; }
        public string? WorkflowLineStepID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}