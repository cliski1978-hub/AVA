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
    /// Updates an existing VaultWorkflowLine's Name, Description, ConditionJson, IsDefaultLine, LineType, LineOrder, WorkflowID, SourceWorkflowNodeID, TargetWorkflowNodeID, or optional identity fields.
    /// </summary>
    public class UpdateVaultWorkflowLineService : ApiServiceBase<UpdateVaultWorkflowLineRequest, UpdateVaultWorkflowLineResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineResponse DoWork(UpdateVaultWorkflowLineRequest request)
        {
            var response = new UpdateVaultWorkflowLineResponse();

            try
            {
                var workflowLine = Context.Set<VaultWorkflowLine>().FirstOrDefault(l => l.ID == request.WorkflowLineID);

                if (workflowLine == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLine '{request.WorkflowLineID}' not found.";
                    return response;
                }

                var workflowID = string.IsNullOrWhiteSpace(request.WorkflowID) ? workflowLine.WorkflowID : request.WorkflowID;
                var sourceWorkflowNodeID = string.IsNullOrWhiteSpace(request.SourceWorkflowNodeID) ? workflowLine.SourceWorkflowNodeID : request.SourceWorkflowNodeID;
                var targetWorkflowNodeID = string.IsNullOrWhiteSpace(request.TargetWorkflowNodeID) ? workflowLine.TargetWorkflowNodeID : request.TargetWorkflowNodeID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowLine.WorkflowID)
                {
                    var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                    if (!workflowExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflow '{request.WorkflowID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.SourceWorkflowNodeID) && request.SourceWorkflowNodeID != workflowLine.SourceWorkflowNodeID)
                {
                    var sourceNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.SourceWorkflowNodeID && n.WorkflowID == workflowID);

                    if (!sourceNodeExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Source VaultWorkflowNode '{request.SourceWorkflowNodeID}' not found for this workflow.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.TargetWorkflowNodeID) && request.TargetWorkflowNodeID != workflowLine.TargetWorkflowNodeID)
                {
                    var targetNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.TargetWorkflowNodeID && n.WorkflowID == workflowID);

                    if (!targetNodeExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Target VaultWorkflowNode '{request.TargetWorkflowNodeID}' not found for this workflow.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var duplicateNameExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID != workflowLine.ID && l.WorkflowID == workflowID && l.SourceWorkflowNodeID == sourceWorkflowNodeID && l.TargetWorkflowNodeID == targetWorkflowNodeID && l.Name.ToLower() == request.Name.ToLower());

                    if (duplicateNameExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A workflow line named '{request.Name}' already exists between these workflow nodes.";
                        return response;
                    }

                    workflowLine.Name = request.Name;
                }

                if (request.Description != null)
                    workflowLine.Description = request.Description;

                if (request.ConditionJson != null)
                    workflowLine.ConditionJson = request.ConditionJson;

                if (request.IsDefaultLine.HasValue)
                    workflowLine.IsDefaultLine = request.IsDefaultLine.Value;

                if (!string.IsNullOrWhiteSpace(request.LineType))
                    workflowLine.LineType = request.LineType;

                if (request.LineOrder.HasValue)
                    workflowLine.LineOrder = request.LineOrder.Value;

                workflowLine.WorkflowID = workflowID;
                workflowLine.SourceWorkflowNodeID = sourceWorkflowNodeID;
                workflowLine.TargetWorkflowNodeID = targetWorkflowNodeID;

                if (request.PrimaryIdentityId != null)
                    workflowLine.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLine.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLine.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLine.IdentityList = request.IdentityList;

                workflowLine.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineService), $"Updated VaultWorkflowLine [{workflowLine.ID}] '{workflowLine.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLine", workflowLine.ID, "Updated");

                response.WorkflowLineID = workflowLine.ID;
                response.WorkflowLine = workflowLine;
                response.UserMessage = "Vault workflow line updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineService), "Error updating VaultWorkflowLine.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultWorkflowLineRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ConditionJson { get; set; }

        public bool? IsDefaultLine { get; set; }

        [MaxLength(64)]
        public string? LineType { get; set; }

        public int? LineOrder { get; set; }

        [MaxLength(128)]
        public string? WorkflowID { get; set; }

        [MaxLength(128)]
        public string? SourceWorkflowNodeID { get; set; }

        [MaxLength(128)]
        public string? TargetWorkflowNodeID { get; set; }

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

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineResponse : CfkApiResponse
    {
        public string? WorkflowLineID { get; set; }
        public VaultWorkflowLine? WorkflowLine { get; set; }
    }

    #endregion
}