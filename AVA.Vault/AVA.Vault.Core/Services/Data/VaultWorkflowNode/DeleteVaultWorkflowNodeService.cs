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
    /// Deletes a VaultWorkflowNode and related workflow line, line step, note-link, and file-ref-link records.
    /// Notes and file references are not deleted because they may be shared outside the workflow node.
    /// </summary>
    public class DeleteVaultWorkflowNodeService : ApiServiceBase<DeleteVaultWorkflowNodeRequest, DeleteVaultWorkflowNodeResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowNodeService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowNodeResponse DoWork(DeleteVaultWorkflowNodeRequest request)
        {
            var response = new DeleteVaultWorkflowNodeResponse();

            try
            {
                var workflowNode = Context.Set<VaultWorkflowNode>().FirstOrDefault(n => n.ID == request.WorkflowNodeID);

                if (workflowNode == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow node not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLines = Context.Set<VaultWorkflowLine>().Where(l => l.SourceWorkflowNodeID == workflowNode.ID || l.TargetWorkflowNodeID == workflowNode.ID).ToList();
                var workflowLineIds = workflowLines.Select(l => l.ID).ToList();

                var workflowLineSteps = Context.Set<VaultWorkflowLineStep>().Where(s => workflowLineIds.Contains(s.WorkflowLineID)).ToList();
                var workflowLineStepIds = workflowLineSteps.Select(s => s.ID).ToList();

                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => workflowLineStepIds.Contains(n.WorkflowLineStepID)).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => workflowLineStepIds.Contains(f.WorkflowLineStepID)).ToList();

                var workflowLineNotes = Context.Set<VaultWorkflowLineNote>().Where(n => workflowLineIds.Contains(n.WorkflowLineID)).ToList();
                var workflowLineFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => workflowLineIds.Contains(f.WorkflowLineID)).ToList();

                var workflowNodeNotes = Context.Set<VaultWorkflowNodeNote>().Where(n => n.WorkflowNodeID == workflowNode.ID).ToList();
                var workflowNodeFileRefs = Context.Set<VaultWorkflowNodeFileRef>().Where(f => f.WorkflowNodeID == workflowNode.ID).ToList();

                foreach (var item in workflowLineStepNotes)
                    Context.Set<VaultWorkflowLineStepNote>().Remove(item);

                foreach (var item in workflowLineStepFileRefs)
                    Context.Set<VaultWorkflowLineStepFileRef>().Remove(item);

                foreach (var item in workflowLineSteps)
                    Context.Set<VaultWorkflowLineStep>().Remove(item);

                foreach (var item in workflowLineNotes)
                    Context.Set<VaultWorkflowLineNote>().Remove(item);

                foreach (var item in workflowLineFileRefs)
                    Context.Set<VaultWorkflowLineFileRef>().Remove(item);

                foreach (var item in workflowLines)
                    Context.Set<VaultWorkflowLine>().Remove(item);

                foreach (var item in workflowNodeNotes)
                    Context.Set<VaultWorkflowNodeNote>().Remove(item);

                foreach (var item in workflowNodeFileRefs)
                    Context.Set<VaultWorkflowNodeFileRef>().Remove(item);

                Context.Set<VaultWorkflowNode>().Remove(workflowNode);
                Context.Flush();

                response.Deleted = true;
                response.UserMessage = "Vault workflow node and related workflow data deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowNodeService), $"Deleted VaultWorkflowNode [{workflowNode.ID}] '{workflowNode.Name}' and related records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNode", workflowNode.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowNodeService), "Error deleting VaultWorkflowNode.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow node.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultWorkflowNodeRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNodeID))
                yield return new ValidationResult("WorkflowNodeID is required.");
        }
    }

    public class DeleteVaultWorkflowNodeResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}