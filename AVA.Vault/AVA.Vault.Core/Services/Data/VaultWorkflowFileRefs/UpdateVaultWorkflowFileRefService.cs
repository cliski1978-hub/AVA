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
    /// Updates an existing VaultWorkflowFileRef link between a VaultWorkflow and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultWorkflowFileRefService : ApiServiceBase<UpdateVaultWorkflowFileRefRequest, UpdateVaultWorkflowFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowFileRefResponse DoWork(UpdateVaultWorkflowFileRefRequest request)
        {
            var response = new UpdateVaultWorkflowFileRefResponse();

            try
            {
                var workflowFileRef = Context.Set<VaultWorkflowFileRef>().FirstOrDefault(f => f.ID == request.WorkflowFileRefID);

                if (workflowFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowFileRef '{request.WorkflowFileRefID}' not found.";
                    return response;
                }

                var workflowID = string.IsNullOrWhiteSpace(request.WorkflowID) ? workflowFileRef.WorkflowID : request.WorkflowID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? workflowFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowFileRef.WorkflowID)
                {
                    var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                    if (!workflowExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflow '{request.WorkflowID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowFileRef.WorkflowID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowFileRef>().Any(f => f.ID != workflowFileRef.ID && f.WorkflowID == workflowID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this workflow.";
                        return response;
                    }
                }

                workflowFileRef.WorkflowID = workflowID;
                workflowFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    workflowFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowFileRef.IdentityList = request.IdentityList;

                workflowFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowFileRefService), $"Updated VaultWorkflowFileRef [{workflowFileRef.ID}] Workflow [{workflowFileRef.WorkflowID}] FileRef [{workflowFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowFileRef", workflowFileRef.ID, "Updated");

                response.WorkflowFileRefID = workflowFileRef.ID;
                response.WorkflowFileRef = workflowFileRef;
                response.UserMessage = "Vault workflow file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowFileRefService), "Error updating VaultWorkflowFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowID { get; set; }

        [MaxLength(128)]
        public string? FileRefID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowFileRefID))
                yield return new ValidationResult("WorkflowFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowFileRefResponse : CfkApiResponse
    {
        public string? WorkflowFileRefID { get; set; }
        public VaultWorkflowFileRef? WorkflowFileRef { get; set; }
    }

    #endregion
}