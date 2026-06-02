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
    /// Deletes a VaultWorkflowFileRef link between a VaultWorkflow and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup should be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowFileRefService : ApiServiceBase<DeleteVaultWorkflowFileRefRequest, DeleteVaultWorkflowFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowFileRefResponse DoWork(DeleteVaultWorkflowFileRefRequest request)
        {
            var response = new DeleteVaultWorkflowFileRefResponse();

            try
            {
                var workflowFileRef = Context.Set<VaultWorkflowFileRef>().FirstOrDefault(f => f.ID == request.WorkflowFileRefID);

                if (workflowFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowId = workflowFileRef.WorkflowID;
                var fileRefId = workflowFileRef.FileRefID;

                Context.Set<VaultWorkflowFileRef>().Remove(workflowFileRef);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowFileRefID = request.WorkflowFileRefID;
                response.WorkflowID = workflowId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault workflow file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowFileRefService), $"Deleted VaultWorkflowFileRef [{request.WorkflowFileRefID}] Workflow [{workflowId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowFileRef", request.WorkflowFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowFileRefService), "Error deleting VaultWorkflowFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowFileRefID))
                yield return new ValidationResult("WorkflowFileRefID is required.");
        }
    }

    public class DeleteVaultWorkflowFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowFileRefID { get; set; }
        public string? WorkflowID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}