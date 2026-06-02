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
    /// Updates an existing VaultWorkflow's Name, Description, WorkflowType, Status, SortOrder, or optional identity fields.
    /// </summary>
    public class UpdateVaultWorkflowService : ApiServiceBase<UpdateVaultWorkflowRequest, UpdateVaultWorkflowResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowResponse DoWork(UpdateVaultWorkflowRequest request)
        {
            var response = new UpdateVaultWorkflowResponse();

            try
            {
                var workflow = Context.Set<VaultWorkflow>().FirstOrDefault(w => w.ID == request.WorkflowID);

                if (workflow == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflow '{request.WorkflowID}' not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.ProjectID) && request.ProjectID != workflow.ProjectID)
                {
                    var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                    if (!projectExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultProject '{request.ProjectID}' not found.";
                        return response;
                    }

                    workflow.ProjectID = request.ProjectID;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var duplicateNameExists = Context.Set<VaultWorkflow>().Any(w => w.ID != workflow.ID && w.ProjectID == workflow.ProjectID && w.Name.ToLower() == request.Name.ToLower());

                    if (duplicateNameExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A workflow named '{request.Name}' already exists for this project.";
                        return response;
                    }

                    workflow.Name = request.Name;
                }

                if (request.Description != null)
                    workflow.Description = request.Description;

                if (!string.IsNullOrWhiteSpace(request.WorkflowType))
                    workflow.WorkflowType = request.WorkflowType;

                if (!string.IsNullOrWhiteSpace(request.Status))
                    workflow.Status = request.Status;

                if (request.SortOrder.HasValue)
                    workflow.SortOrder = request.SortOrder.Value;

                if (request.PrimaryIdentityId != null)
                    workflow.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflow.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflow.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflow.IdentityList = request.IdentityList;

                workflow.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowService), $"Updated VaultWorkflow [{workflow.ID}] '{workflow.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflow", workflow.ID, "Updated");

                response.WorkflowID = workflow.ID;
                response.Workflow = workflow;
                response.UserMessage = "Vault workflow updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowService), "Error updating VaultWorkflow.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultWorkflowRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowID { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(64)]
        public string? WorkflowType { get; set; }

        [MaxLength(64)]
        public string? Status { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowID))
                yield return new ValidationResult("WorkflowID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowResponse : CfkApiResponse
    {
        public string? WorkflowID { get; set; }
        public VaultWorkflow? Workflow { get; set; }
    }

    #endregion
}