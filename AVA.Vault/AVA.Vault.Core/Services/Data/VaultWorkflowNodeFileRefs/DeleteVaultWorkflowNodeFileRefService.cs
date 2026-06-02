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
    /// Deletes a VaultWorkflowNodeFileRef link between a VaultWorkflowNode and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowNodeFileRefService : ApiServiceBase<DeleteVaultWorkflowNodeFileRefRequest, DeleteVaultWorkflowNodeFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowNodeFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowNodeFileRefResponse DoWork(DeleteVaultWorkflowNodeFileRefRequest request)
        {
            var response = new DeleteVaultWorkflowNodeFileRefResponse();

            try
            {
                var workflowNodeFileRef = Context.Set<VaultWorkflowNodeFileRef>().FirstOrDefault(f => f.ID == request.WorkflowNodeFileRefID);

                if (workflowNodeFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow node file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowNodeId = workflowNodeFileRef.WorkflowNodeID;
                var fileRefId = workflowNodeFileRef.FileRefID;

                Context.Set<VaultWorkflowNodeFileRef>().Remove(workflowNodeFileRef);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowNodeFileRefID = request.WorkflowNodeFileRefID;
                response.WorkflowNodeID = workflowNodeId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault workflow node file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowNodeFileRefService), $"Deleted VaultWorkflowNodeFileRef [{request.WorkflowNodeFileRefID}] WorkflowNode [{workflowNodeId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeFileRef", request.WorkflowNodeFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowNodeFileRefService), "Error deleting VaultWorkflowNodeFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow node file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowNodeFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNodeFileRefID))
                yield return new ValidationResult("WorkflowNodeFileRefID is required.");
        }
    }

    public class DeleteVaultWorkflowNodeFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowNodeFileRefID { get; set; }
        public string? WorkflowNodeID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}