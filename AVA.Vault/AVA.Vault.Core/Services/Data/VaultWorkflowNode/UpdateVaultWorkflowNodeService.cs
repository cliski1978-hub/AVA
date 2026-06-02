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
    /// Updates an existing VaultWorkflowNode's Name, Description, Instructions, MetadataJson, NodeType, NodeOrder, Status, WorkflowID, or optional identity fields.
    /// </summary>
    public class UpdateVaultWorkflowNodeService : ApiServiceBase<UpdateVaultWorkflowNodeRequest, UpdateVaultWorkflowNodeResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowNodeService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowNodeResponse DoWork(UpdateVaultWorkflowNodeRequest request)
        {
            var response = new UpdateVaultWorkflowNodeResponse();

            try
            {
                var workflowNode = Context.Set<VaultWorkflowNode>().FirstOrDefault(n => n.ID == request.WorkflowNodeID);

                if (workflowNode == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNode '{request.WorkflowNodeID}' not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowNode.WorkflowID)
                {
                    var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                    if (!workflowExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflow '{request.WorkflowID}' not found.";
                        return response;
                    }

                    workflowNode.WorkflowID = request.WorkflowID;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var duplicateNameExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID != workflowNode.ID && n.WorkflowID == workflowNode.WorkflowID && n.Name.ToLower() == request.Name.ToLower());

                    if (duplicateNameExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A workflow node named '{request.Name}' already exists for this workflow.";
                        return response;
                    }

                    workflowNode.Name = request.Name;
                }

                if (request.Description != null)
                    workflowNode.Description = request.Description;

                if (request.Instructions != null)
                    workflowNode.Instructions = request.Instructions;

                if (request.MetadataJson != null)
                    workflowNode.MetadataJson = request.MetadataJson;

                if (!string.IsNullOrWhiteSpace(request.NodeType))
                    workflowNode.NodeType = request.NodeType;

                if (request.NodeOrder.HasValue)
                    workflowNode.NodeOrder = request.NodeOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.Status))
                    workflowNode.Status = request.Status;

                if (request.PrimaryIdentityId != null)
                    workflowNode.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowNode.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowNode.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowNode.IdentityList = request.IdentityList;

                workflowNode.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowNodeService), $"Updated VaultWorkflowNode [{workflowNode.ID}] '{workflowNode.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNode", workflowNode.ID, "Updated");

                response.WorkflowNodeID = workflowNode.ID;
                response.WorkflowNode = workflowNode;
                response.UserMessage = "Vault workflow node updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowNodeService), "Error updating VaultWorkflowNode.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow node.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultWorkflowNodeRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeID { get; set; }

        [MaxLength(128)]
        public string? WorkflowID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Instructions { get; set; }

        public string? MetadataJson { get; set; }

        [MaxLength(64)]
        public string? NodeType { get; set; }

        public int? NodeOrder { get; set; }

        [MaxLength(64)]
        public string? Status { get; set; }

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

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowNodeResponse : CfkApiResponse
    {
        public string? WorkflowNodeID { get; set; }
        public VaultWorkflowNode? WorkflowNode { get; set; }
    }

    #endregion
}