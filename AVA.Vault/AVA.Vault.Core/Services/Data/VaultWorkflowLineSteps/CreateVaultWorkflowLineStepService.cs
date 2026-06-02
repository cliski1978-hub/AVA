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
    /// Creates and persists a new VaultWorkflowLineStep.
    /// </summary>
    public class CreateVaultWorkflowLineStepService : ApiServiceBase<CreateVaultWorkflowLineStepRequest, CreateVaultWorkflowLineStepResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineStepService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineStepResponse DoWork(CreateVaultWorkflowLineStepRequest request)
        {
            var response = new CreateVaultWorkflowLineStepResponse();

            try
            {
                var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                if (!workflowLineExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLine [{request.WorkflowLineID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID == request.WorkflowLineStepID || (s.WorkflowLineID == request.WorkflowLineID && s.StepOrder == request.StepOrder));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A workflow line step with order [{request.StepOrder}] already exists for this workflow line.";
                    return response;
                }

                var workflowLineStep = new VaultWorkflowLineStep
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineStepID) ? Guid.NewGuid().ToString() : request.WorkflowLineStepID,
                    Name = request.Name,
                    Description = request.Description,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    StepOrder = request.StepOrder,
                    StepType = string.IsNullOrWhiteSpace(request.StepType) ? "General" : request.StepType,
                    WorkflowLineID = request.WorkflowLineID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLineStep>().Add(workflowLineStep);
                Context.Flush();

                // Set response before logging - if logging fails the created entity is still returned
                response.WorkflowLineStepID = workflowLineStep.ID;
                response.WorkflowLineStep = workflowLineStep;
                response.UserMessage = "Vault workflow line step created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineStepService), $"Created VaultWorkflowLineStep [{workflowLineStep.ID}] '{workflowLineStep.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStep", workflowLineStep.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineStepService), "Error creating VaultWorkflowLineStep.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line step.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultWorkflowLineStepRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineStepID { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int StepOrder { get; set; }

        [MaxLength(64)]
        public string? StepType { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowLineID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");

            if (string.IsNullOrWhiteSpace(WorkflowLineID))
                yield return new ValidationResult("WorkflowLineID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowLineStepResponse : CfkApiResponse
    {
        public string? WorkflowLineStepID { get; set; }
        public VaultWorkflowLineStep? WorkflowLineStep { get; set; }
    }

    #endregion
}