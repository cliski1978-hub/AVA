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
    /// Updates an existing VaultWorkflowNodeFileRef link between a VaultWorkflowNode and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultWorkflowNodeFileRefService : ApiServiceBase<UpdateVaultWorkflowNodeFileRefRequest, UpdateVaultWorkflowNodeFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowNodeFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowNodeFileRefResponse DoWork(UpdateVaultWorkflowNodeFileRefRequest request)
        {
            var response = new UpdateVaultWorkflowNodeFileRefResponse();

            try
            {
                var workflowNodeFileRef = Context.Set<VaultWorkflowNodeFileRef>().FirstOrDefault(f => f.ID == request.WorkflowNodeFileRefID);

                if (workflowNodeFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNodeFileRef '{request.WorkflowNodeFileRefID}' not found.";
                    return response;
                }

                var workflowNodeID = string.IsNullOrWhiteSpace(request.WorkflowNodeID) ? workflowNodeFileRef.WorkflowNodeID : request.WorkflowNodeID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? workflowNodeFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowNodeID) && request.WorkflowNodeID != workflowNodeFileRef.WorkflowNodeID)
                {
                    var workflowNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.WorkflowNodeID);

                    if (!workflowNodeExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowNode '{request.WorkflowNodeID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowNodeFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowNodeID) && request.WorkflowNodeID != workflowNodeFileRef.WorkflowNodeID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowNodeFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowNodeFileRef>().Any(f => f.ID != workflowNodeFileRef.ID && f.WorkflowNodeID == workflowNodeID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this workflow node.";
                        return response;
                    }
                }

                workflowNodeFileRef.WorkflowNodeID = workflowNodeID;
                workflowNodeFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    workflowNodeFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowNodeFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowNodeFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowNodeFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowNodeFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowNodeFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowNodeFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowNodeFileRef.IdentityList = request.IdentityList;

                workflowNodeFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowNodeFileRefService), $"Updated VaultWorkflowNodeFileRef [{workflowNodeFileRef.ID}] WorkflowNode [{workflowNodeFileRef.WorkflowNodeID}] FileRef [{workflowNodeFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeFileRef", workflowNodeFileRef.ID, "Updated");

                response.WorkflowNodeFileRefID = workflowNodeFileRef.ID;
                response.WorkflowNodeFileRef = workflowNodeFileRef;
                response.UserMessage = "Vault workflow node file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowNodeFileRefService), "Error updating VaultWorkflowNodeFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow node file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowNodeFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowNodeID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowNodeFileRefID))
                yield return new ValidationResult("WorkflowNodeFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowNodeFileRefResponse : CfkApiResponse
    {
        public string? WorkflowNodeFileRefID { get; set; }
        public VaultWorkflowNodeFileRef? WorkflowNodeFileRef { get; set; }
    }

    #endregion
}