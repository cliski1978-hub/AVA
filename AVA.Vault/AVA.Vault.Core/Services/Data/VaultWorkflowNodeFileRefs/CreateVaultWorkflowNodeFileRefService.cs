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
    /// Creates and persists a new VaultWorkflowNodeFileRef link between a VaultWorkflowNode and VaultFileRef.
    /// </summary>
    public class CreateVaultWorkflowNodeFileRefService : ApiServiceBase<CreateVaultWorkflowNodeFileRefRequest, CreateVaultWorkflowNodeFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowNodeFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowNodeFileRefResponse DoWork(CreateVaultWorkflowNodeFileRefRequest request)
        {
            var response = new CreateVaultWorkflowNodeFileRefResponse();

            try
            {
                var workflowNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.WorkflowNodeID);

                if (!workflowNodeExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNode [{request.WorkflowNodeID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowNodeFileRef>().Any(f => f.ID == request.WorkflowNodeFileRefID || (f.WorkflowNodeID == request.WorkflowNodeID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this workflow node.";
                    return response;
                }

                var workflowNodeFileRef = new VaultWorkflowNodeFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowNodeFileRefID) ? Guid.NewGuid().ToString() : request.WorkflowNodeFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowNodeID = request.WorkflowNodeID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowNodeFileRef>().Add(workflowNodeFileRef);
                Context.Flush();

                response.WorkflowNodeFileRefID = workflowNodeFileRef.ID;
                response.WorkflowNodeFileRef = workflowNodeFileRef;
                response.UserMessage = "Vault workflow node file reference link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowNodeFileRefService), $"Created VaultWorkflowNodeFileRef [{workflowNodeFileRef.ID}] WorkflowNode [{workflowNodeFileRef.WorkflowNodeID}] FileRef [{workflowNodeFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeFileRef", workflowNodeFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowNodeFileRefService), "Error creating VaultWorkflowNodeFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow node file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowNodeFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowNodeFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowNodeID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowNodeID))
                yield return new ValidationResult("WorkflowNodeID is required.");

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

    public class CreateVaultWorkflowNodeFileRefResponse : CfkApiResponse
    {
        public string? WorkflowNodeFileRefID { get; set; }
        public VaultWorkflowNodeFileRef? WorkflowNodeFileRef { get; set; }
    }

    #endregion
}