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
    /// Updates an existing VaultWorkflowLineStepFileRef link between a VaultWorkflowLineStep and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultWorkflowLineStepFileRefService : ApiServiceBase<UpdateVaultWorkflowLineStepFileRefRequest, UpdateVaultWorkflowLineStepFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineStepFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineStepFileRefResponse DoWork(UpdateVaultWorkflowLineStepFileRefRequest request)
        {
            var response = new UpdateVaultWorkflowLineStepFileRefResponse();

            try
            {
                var workflowLineStepFileRef = Context.Set<VaultWorkflowLineStepFileRef>().FirstOrDefault(f => f.ID == request.WorkflowLineStepFileRefID);

                if (workflowLineStepFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineStepFileRef '{request.WorkflowLineStepFileRefID}' not found.";
                    return response;
                }

                var workflowLineStepID = string.IsNullOrWhiteSpace(request.WorkflowLineStepID) ? workflowLineStepFileRef.WorkflowLineStepID : request.WorkflowLineStepID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? workflowLineStepFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowLineStepID) && request.WorkflowLineStepID != workflowLineStepFileRef.WorkflowLineStepID)
                {
                    var workflowLineStepExists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID == request.WorkflowLineStepID);

                    if (!workflowLineStepExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowLineStep '{request.WorkflowLineStepID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowLineStepFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowLineStepID) && request.WorkflowLineStepID != workflowLineStepFileRef.WorkflowLineStepID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowLineStepFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowLineStepFileRef>().Any(f => f.ID != workflowLineStepFileRef.ID && f.WorkflowLineStepID == workflowLineStepID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this workflow line step.";
                        return response;
                    }
                }

                workflowLineStepFileRef.WorkflowLineStepID = workflowLineStepID;
                workflowLineStepFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    workflowLineStepFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowLineStepFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowLineStepFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowLineStepFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowLineStepFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLineStepFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLineStepFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLineStepFileRef.IdentityList = request.IdentityList;

                workflowLineStepFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineStepFileRefService), $"Updated VaultWorkflowLineStepFileRef [{workflowLineStepFileRef.ID}] WorkflowLineStep [{workflowLineStepFileRef.WorkflowLineStepID}] FileRef [{workflowLineStepFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepFileRef", workflowLineStepFileRef.ID, "Updated");

                response.WorkflowLineStepFileRefID = workflowLineStepFileRef.ID;
                response.WorkflowLineStepFileRef = workflowLineStepFileRef;
                response.UserMessage = "Vault workflow line step file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineStepFileRefService), "Error updating VaultWorkflowLineStepFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line step file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowLineStepFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowLineStepID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineStepFileRefID))
                yield return new ValidationResult("WorkflowLineStepFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineStepFileRefResponse : CfkApiResponse
    {
        public string? WorkflowLineStepFileRefID { get; set; }
        public VaultWorkflowLineStepFileRef? WorkflowLineStepFileRef { get; set; }
    }

    #endregion
}