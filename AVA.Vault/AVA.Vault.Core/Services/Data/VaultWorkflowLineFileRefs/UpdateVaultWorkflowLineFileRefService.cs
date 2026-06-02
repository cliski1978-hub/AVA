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
    /// Updates an existing VaultWorkflowLineFileRef link between a VaultWorkflowLine and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultWorkflowLineFileRefService : ApiServiceBase<UpdateVaultWorkflowLineFileRefRequest, UpdateVaultWorkflowLineFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineFileRefResponse DoWork(UpdateVaultWorkflowLineFileRefRequest request)
        {
            var response = new UpdateVaultWorkflowLineFileRefResponse();

            try
            {
                var workflowLineFileRef = Context.Set<VaultWorkflowLineFileRef>().FirstOrDefault(f => f.ID == request.WorkflowLineFileRefID);

                if (workflowLineFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineFileRef '{request.WorkflowLineFileRefID}' not found.";
                    return response;
                }

                var workflowLineID = string.IsNullOrWhiteSpace(request.WorkflowLineID) ? workflowLineFileRef.WorkflowLineID : request.WorkflowLineID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? workflowLineFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowLineID) && request.WorkflowLineID != workflowLineFileRef.WorkflowLineID)
                {
                    var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                    if (!workflowLineExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowLine '{request.WorkflowLineID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowLineFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowLineID) && request.WorkflowLineID != workflowLineFileRef.WorkflowLineID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != workflowLineFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowLineFileRef>().Any(f => f.ID != workflowLineFileRef.ID && f.WorkflowLineID == workflowLineID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this workflow line.";
                        return response;
                    }
                }

                workflowLineFileRef.WorkflowLineID = workflowLineID;
                workflowLineFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    workflowLineFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowLineFileRef.IsRequired = request.IsRequired.Value;

                if (request.FileOrder.HasValue)
                    workflowLineFileRef.FileOrder = request.FileOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowLineFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowLineFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLineFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLineFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLineFileRef.IdentityList = request.IdentityList;

                workflowLineFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineFileRefService), $"Updated VaultWorkflowLineFileRef [{workflowLineFileRef.ID}] WorkflowLine [{workflowLineFileRef.WorkflowLineID}] FileRef [{workflowLineFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineFileRef", workflowLineFileRef.ID, "Updated");

                response.WorkflowLineFileRefID = workflowLineFileRef.ID;
                response.WorkflowLineFileRef = workflowLineFileRef;
                response.UserMessage = "Vault workflow line file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineFileRefService), "Error updating VaultWorkflowLineFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowLineFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? FileOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowLineID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineFileRefID))
                yield return new ValidationResult("WorkflowLineFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineFileRefResponse : CfkApiResponse
    {
        public string? WorkflowLineFileRefID { get; set; }
        public VaultWorkflowLineFileRef? WorkflowLineFileRef { get; set; }
    }

    #endregion
}