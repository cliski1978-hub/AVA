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
    /// Creates and persists a new VaultWorkflowLineFileRef link between a VaultWorkflowLine and VaultFileRef.
    /// </summary>
    public class CreateVaultWorkflowLineFileRefService : ApiServiceBase<CreateVaultWorkflowLineFileRefRequest, CreateVaultWorkflowLineFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineFileRefResponse DoWork(CreateVaultWorkflowLineFileRefRequest request)
        {
            var response = new CreateVaultWorkflowLineFileRefResponse();

            try
            {
                var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                if (!workflowLineExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLine [{request.WorkflowLineID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLineFileRef>().Any(f => f.ID == request.WorkflowLineFileRefID || (f.WorkflowLineID == request.WorkflowLineID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this workflow line.";
                    return response;
                }

                var workflowLineFileRef = new VaultWorkflowLineFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineFileRefID) ? Guid.NewGuid().ToString() : request.WorkflowLineFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    FileOrder = request.FileOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowLineID = request.WorkflowLineID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLineFileRef>().Add(workflowLineFileRef);
                Context.Flush();

                response.WorkflowLineFileRefID = workflowLineFileRef.ID;
                response.WorkflowLineFileRef = workflowLineFileRef;
                response.UserMessage = "Vault workflow line file reference link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineFileRefService), $"Created VaultWorkflowLineFileRef [{workflowLineFileRef.ID}] WorkflowLine [{workflowLineFileRef.WorkflowLineID}] FileRef [{workflowLineFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineFileRef", workflowLineFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineFileRefService), "Error creating VaultWorkflowLineFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowLineFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int FileOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowLineID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineID))
                yield return new ValidationResult("WorkflowLineID is required.");

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

    public class CreateVaultWorkflowLineFileRefResponse : CfkApiResponse
    {
        public string? WorkflowLineFileRefID { get; set; }
        public VaultWorkflowLineFileRef? WorkflowLineFileRef { get; set; }
    }

    #endregion
}