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
    /// Creates and persists a new VaultWorkflowFileRef link between a VaultWorkflow and VaultFileRef.
    /// </summary>
    public class CreateVaultWorkflowFileRefService : ApiServiceBase<CreateVaultWorkflowFileRefRequest, CreateVaultWorkflowFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowFileRefResponse DoWork(CreateVaultWorkflowFileRefRequest request)
        {
            var response = new CreateVaultWorkflowFileRefResponse();

            try
            {
                var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                if (!workflowExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflow [{request.WorkflowID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowFileRef>().Any(f => f.ID == request.WorkflowFileRefID || (f.WorkflowID == request.WorkflowID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this workflow.";
                    return response;
                }

                var workflowFileRef = new VaultWorkflowFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowFileRefID) ? Guid.NewGuid().ToString() : request.WorkflowFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowID = request.WorkflowID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowFileRef>().Add(workflowFileRef);
                Context.Flush();

                response.WorkflowFileRefID = workflowFileRef.ID;
                response.WorkflowFileRef = workflowFileRef;
                response.UserMessage = "Vault workflow file reference link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowFileRefService), $"Created VaultWorkflowFileRef [{workflowFileRef.ID}] Workflow [{workflowFileRef.WorkflowID}] FileRef [{workflowFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowFileRef", workflowFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowFileRefService), "Error creating VaultWorkflowFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowID))
                yield return new ValidationResult("WorkflowID is required.");

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

    public class CreateVaultWorkflowFileRefResponse : CfkApiResponse
    {
        public string? WorkflowFileRefID { get; set; }
        public VaultWorkflowFileRef? WorkflowFileRef { get; set; }
    }

    #endregion
}