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
    /// Creates and persists a new VaultWorkflowLineStepFileRef link between a VaultWorkflowLineStep and VaultFileRef.
    /// </summary>
    public class CreateVaultWorkflowLineStepFileRefService : ApiServiceBase<CreateVaultWorkflowLineStepFileRefRequest, CreateVaultWorkflowLineStepFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineStepFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineStepFileRefResponse DoWork(CreateVaultWorkflowLineStepFileRefRequest request)
        {
            var response = new CreateVaultWorkflowLineStepFileRefResponse();

            try
            {
                var workflowLineStepExists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID == request.WorkflowLineStepID);

                if (!workflowLineStepExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineStep [{request.WorkflowLineStepID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLineStepFileRef>().Any(f => f.ID == request.WorkflowLineStepFileRefID || (f.WorkflowLineStepID == request.WorkflowLineStepID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this workflow line step.";
                    return response;
                }

                var workflowLineStepFileRef = new VaultWorkflowLineStepFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineStepFileRefID) ? Guid.NewGuid().ToString() : request.WorkflowLineStepFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowLineStepID = request.WorkflowLineStepID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLineStepFileRef>().Add(workflowLineStepFileRef);
                Context.Flush();

                response.WorkflowLineStepFileRefID = workflowLineStepFileRef.ID;
                response.WorkflowLineStepFileRef = workflowLineStepFileRef;
                response.UserMessage = "Vault workflow line step file reference link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineStepFileRefService), $"Created VaultWorkflowLineStepFileRef [{workflowLineStepFileRef.ID}] WorkflowLineStep [{workflowLineStepFileRef.WorkflowLineStepID}] FileRef [{workflowLineStepFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepFileRef", workflowLineStepFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineStepFileRefService), "Error creating VaultWorkflowLineStepFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line step file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowLineStepFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineStepFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowLineStepID { get; set; }

        [Required]
        [MaxLength(128)]
        public string FileRefID { get; set; }

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

            if (string.IsNullOrWhiteSpace(FileRefID))
                yield return new ValidationResult("FileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowLineStepFileRefResponse : CfkApiResponse
    {
        public string? WorkflowLineStepFileRefID { get; set; }
        public VaultWorkflowLineStepFileRef? WorkflowLineStepFileRef { get; set; }
    }

    #endregion
}