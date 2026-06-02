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
    /// Creates and persists a new VaultWorkflowLine.
    /// </summary>
    public class CreateVaultWorkflowLineService : ApiServiceBase<CreateVaultWorkflowLineRequest, CreateVaultWorkflowLineResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineResponse DoWork(CreateVaultWorkflowLineRequest request)
        {
            var response = new CreateVaultWorkflowLineResponse();

            try
            {
                var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                if (!workflowExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflow [{request.WorkflowID}] was not found.";
                    return response;
                }

                var sourceNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.SourceWorkflowNodeID && n.WorkflowID == request.WorkflowID);

                if (!sourceNodeExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Source VaultWorkflowNode [{request.SourceWorkflowNodeID}] was not found for this workflow.";
                    return response;
                }

                var targetNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.TargetWorkflowNodeID && n.WorkflowID == request.WorkflowID);

                if (!targetNodeExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Target VaultWorkflowNode [{request.TargetWorkflowNodeID}] was not found for this workflow.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID || (l.WorkflowID == request.WorkflowID && l.SourceWorkflowNodeID == request.SourceWorkflowNodeID && l.TargetWorkflowNodeID == request.TargetWorkflowNodeID && l.Name.ToLower() == request.Name.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A workflow line named '{request.Name}' already exists between these workflow nodes.";
                    return response;
                }

                var workflowLine = new VaultWorkflowLine
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineID) ? Guid.NewGuid().ToString() : request.WorkflowLineID,
                    Name = request.Name,
                    Description = request.Description,
                    ConditionJson = request.ConditionJson,
                    IsDefaultLine = request.IsDefaultLine,
                    LineType = string.IsNullOrWhiteSpace(request.LineType) ? "General" : request.LineType,
                    LineOrder = request.LineOrder,
                    WorkflowID = request.WorkflowID,
                    SourceWorkflowNodeID = request.SourceWorkflowNodeID,
                    TargetWorkflowNodeID = request.TargetWorkflowNodeID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLine>().Add(workflowLine);
                Context.Flush();

                // Set response before logging - if logging fails the created entity is still returned
                response.WorkflowLineID = workflowLine.ID;
                response.WorkflowLine = workflowLine;
                response.UserMessage = "Vault workflow line created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineService), $"Created VaultWorkflowLine [{workflowLine.ID}] '{workflowLine.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLine", workflowLine.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineService), "Error creating VaultWorkflowLine.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultWorkflowLineRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineID { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? ConditionJson { get; set; }

        public bool IsDefaultLine { get; set; }

        [MaxLength(64)]
        public string? LineType { get; set; }

        public int LineOrder { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; }

        [Required]
        [MaxLength(128)]
        public string SourceWorkflowNodeID { get; set; }

        [Required]
        [MaxLength(128)]
        public string TargetWorkflowNodeID { get; set; }

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

            if (string.IsNullOrWhiteSpace(WorkflowID))
                yield return new ValidationResult("WorkflowID is required.");

            if (string.IsNullOrWhiteSpace(SourceWorkflowNodeID))
                yield return new ValidationResult("SourceWorkflowNodeID is required.");

            if (string.IsNullOrWhiteSpace(TargetWorkflowNodeID))
                yield return new ValidationResult("TargetWorkflowNodeID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowLineResponse : CfkApiResponse
    {
        public string? WorkflowLineID { get; set; }
        public VaultWorkflowLine? WorkflowLine { get; set; }
    }

    #endregion
}