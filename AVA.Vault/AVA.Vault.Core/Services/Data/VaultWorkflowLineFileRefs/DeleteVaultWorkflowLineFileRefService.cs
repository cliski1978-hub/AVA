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
    /// Deletes a VaultWorkflowLineFileRef link between a VaultWorkflowLine and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowLineFileRefService : ApiServiceBase<DeleteVaultWorkflowLineFileRefRequest, DeleteVaultWorkflowLineFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineFileRefResponse DoWork(DeleteVaultWorkflowLineFileRefRequest request)
        {
            var response = new DeleteVaultWorkflowLineFileRefResponse();

            try
            {
                var workflowLineFileRef = Context.Set<VaultWorkflowLineFileRef>().FirstOrDefault(f => f.ID == request.WorkflowLineFileRefID);

                if (workflowLineFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineId = workflowLineFileRef.WorkflowLineID;
                var fileRefId = workflowLineFileRef.FileRefID;

                Context.Set<VaultWorkflowLineFileRef>().Remove(workflowLineFileRef);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowLineFileRefID = request.WorkflowLineFileRefID;
                response.WorkflowLineID = workflowLineId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault workflow line file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineFileRefService), $"Deleted VaultWorkflowLineFileRef [{request.WorkflowLineFileRefID}] WorkflowLine [{workflowLineId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineFileRef", request.WorkflowLineFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineFileRefService), "Error deleting VaultWorkflowLineFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowLineFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineFileRefID))
                yield return new ValidationResult("WorkflowLineFileRefID is required.");
        }
    }

    public class DeleteVaultWorkflowLineFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowLineFileRefID { get; set; }
        public string? WorkflowLineID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}