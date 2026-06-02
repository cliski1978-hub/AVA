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
    /// Creates and persists a new VaultWorkflowNode.
    /// </summary>
    public class CreateVaultWorkflowNodeService : ApiServiceBase<CreateVaultWorkflowNodeRequest, CreateVaultWorkflowNodeResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowNodeService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowNodeResponse DoWork(CreateVaultWorkflowNodeRequest request)
        {
            var response = new CreateVaultWorkflowNodeResponse();

            try
            {
                var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                if (!workflowExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflow [{request.WorkflowID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.WorkflowNodeID || (n.WorkflowID == request.WorkflowID && n.Name.ToLower() == request.Name.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A workflow node named '{request.Name}' already exists for this workflow.";
                    return response;
                }

                var workflowNode = new VaultWorkflowNode
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowNodeID) ? Guid.NewGuid().ToString() : request.WorkflowNodeID,
                    Name = request.Name,
                    Description = request.Description,
                    Instructions = request.Instructions,
                    MetadataJson = request.MetadataJson,
                    NodeType = string.IsNullOrWhiteSpace(request.NodeType) ? "General" : request.NodeType,
                    NodeOrder = request.NodeOrder,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
                    WorkflowID = request.WorkflowID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowNode>().Add(workflowNode);
                Context.Flush();

                // Set response before logging - if logging fails the created entity is still returned
                response.WorkflowNodeID = workflowNode.ID;
                response.WorkflowNode = workflowNode;
                response.UserMessage = "Vault workflow node created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowNodeService), $"Created VaultWorkflowNode [{workflowNode.ID}] '{workflowNode.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNode", workflowNode.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowNodeService), "Error creating VaultWorkflowNode.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow node.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultWorkflowNodeRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowNodeID { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? Instructions { get; set; }

        public string? MetadataJson { get; set; }

        [MaxLength(64)]
        public string? NodeType { get; set; }

        public int NodeOrder { get; set; }

        [MaxLength(64)]
        public string? Status { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; }

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

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowNodeResponse : CfkApiResponse
    {
        public string? WorkflowNodeID { get; set; }
        public VaultWorkflowNode? WorkflowNode { get; set; }
    }

    #endregion
}