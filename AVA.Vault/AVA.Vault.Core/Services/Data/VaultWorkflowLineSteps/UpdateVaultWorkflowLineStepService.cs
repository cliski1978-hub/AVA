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
    /// Updates an existing VaultWorkflowLineStep's Name, Description, Instructions, IsRequired, StepOrder, StepType, WorkflowLineID, or optional identity fields.
    /// </summary>
    public class UpdateVaultWorkflowLineStepService : ApiServiceBase<UpdateVaultWorkflowLineStepRequest, UpdateVaultWorkflowLineStepResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineStepService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineStepResponse DoWork(UpdateVaultWorkflowLineStepRequest request)
        {
            var response = new UpdateVaultWorkflowLineStepResponse();

            try
            {
                var workflowLineStep = Context.Set<VaultWorkflowLineStep>().FirstOrDefault(s => s.ID == request.WorkflowLineStepID);

                if (workflowLineStep == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineStep '{request.WorkflowLineStepID}' not found.";
                    return response;
                }

                var workflowLineID = string.IsNullOrWhiteSpace(request.WorkflowLineID) ? workflowLineStep.WorkflowLineID : request.WorkflowLineID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowLineID) && request.WorkflowLineID != workflowLineStep.WorkflowLineID)
                {
                    var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                    if (!workflowLineExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowLine '{request.WorkflowLineID}' not found.";
                        return response;
                    }
                }

                if (request.StepOrder.HasValue)
                {
                    var duplicateStepOrderExists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID != workflowLineStep.ID && s.WorkflowLineID == workflowLineID && s.StepOrder == request.StepOrder.Value);

                    if (duplicateStepOrderExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A workflow line step with order [{request.StepOrder.Value}] already exists for this workflow line.";
                        return response;
                    }

                    workflowLineStep.StepOrder = request.StepOrder.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                    workflowLineStep.Name = request.Name;

                if (request.Description != null)
                    workflowLineStep.Description = request.Description;

                if (request.Instructions != null)
                    workflowLineStep.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowLineStep.IsRequired = request.IsRequired.Value;

                if (!string.IsNullOrWhiteSpace(request.StepType))
                    workflowLineStep.StepType = request.StepType;

                workflowLineStep.WorkflowLineID = workflowLineID;

                if (request.PrimaryIdentityId != null)
                    workflowLineStep.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLineStep.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLineStep.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLineStep.IdentityList = request.IdentityList;

                workflowLineStep.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineStepService), $"Updated VaultWorkflowLineStep [{workflowLineStep.ID}] '{workflowLineStep.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStep", workflowLineStep.ID, "Updated");

                response.WorkflowLineStepID = workflowLineStep.ID;
                response.WorkflowLineStep = workflowLineStep;
                response.UserMessage = "Vault workflow line step updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineStepService), "Error updating VaultWorkflowLineStep.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line step.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultWorkflowLineStepRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? StepOrder { get; set; }

        [MaxLength(64)]
        public string? StepType { get; set; }

        [MaxLength(128)]
        public string? WorkflowLineID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineStepID))
                yield return new ValidationResult("WorkflowLineStepID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineStepResponse : CfkApiResponse
    {
        public string? WorkflowLineStepID { get; set; }
        public VaultWorkflowLineStep? WorkflowLineStep { get; set; }
    }

    #endregion
}