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
    /// Deletes a VaultWorkflow and related workflow nodes, lines, line steps, and workflow relation records.
    /// Notes and file references are not deleted because they may be shared outside the workflow.
    /// </summary>
    public class DeleteVaultWorkflowService : ApiServiceBase<DeleteVaultWorkflowRequest, DeleteVaultWorkflowResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowResponse DoWork(DeleteVaultWorkflowRequest request)
        {
            var response = new DeleteVaultWorkflowResponse();

            try
            {
                var workflow = Context.Set<VaultWorkflow>().FirstOrDefault(w => w.ID == request.WorkflowID);

                if (workflow == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowNodes = Context.Set<VaultWorkflowNode>().Where(n => n.WorkflowID == workflow.ID).ToList();
                var workflowNodeIds = workflowNodes.Select(n => n.ID).ToList();

                var workflowLines = Context.Set<VaultWorkflowLine>().Where(l => l.WorkflowID == workflow.ID || workflowNodeIds.Contains(l.SourceWorkflowNodeID) || workflowNodeIds.Contains(l.TargetWorkflowNodeID)).ToList();
                var workflowLineIds = workflowLines.Select(l => l.ID).ToList();

                var workflowLineSteps = Context.Set<VaultWorkflowLineStep>().Where(s => workflowLineIds.Contains(s.WorkflowLineID)).ToList();
                var workflowLineStepIds = workflowLineSteps.Select(s => s.ID).ToList();

                var workflowNotes = Context.Set<VaultWorkflowNote>().Where(n => n.WorkflowID == workflow.ID).ToList();
                var workflowFileRefs = Context.Set<VaultWorkflowFileRef>().Where(f => f.WorkflowID == workflow.ID).ToList();

                var workflowNodeNotes = Context.Set<VaultWorkflowNodeNote>().Where(n => workflowNodeIds.Contains(n.WorkflowNodeID)).ToList();
                var workflowNodeFileRefs = Context.Set<VaultWorkflowNodeFileRef>().Where(f => workflowNodeIds.Contains(f.WorkflowNodeID)).ToList();

                var workflowLineNotes = Context.Set<VaultWorkflowLineNote>().Where(n => workflowLineIds.Contains(n.WorkflowLineID)).ToList();
                var workflowLineFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => workflowLineIds.Contains(f.WorkflowLineID)).ToList();

                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => workflowLineStepIds.Contains(n.WorkflowLineStepID)).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => workflowLineStepIds.Contains(f.WorkflowLineStepID)).ToList();

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

                foreach (var item in workflowNodes)
                    Context.Set<VaultWorkflowNode>().Remove(item);

                foreach (var item in workflowNotes)
                    Context.Set<VaultWorkflowNote>().Remove(item);

                foreach (var item in workflowFileRefs)
                    Context.Set<VaultWorkflowFileRef>().Remove(item);

                Context.Set<VaultWorkflow>().Remove(workflow);
                Context.Flush();

                response.Deleted = true;
                response.UserMessage = "Vault workflow and related workflow data deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowService), $"Deleted VaultWorkflow [{workflow.ID}] '{workflow.Name}' and related workflow records.");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflow", workflow.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowService), "Error deleting VaultWorkflow.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultWorkflowRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowID))
                yield return new ValidationResult("WorkflowID is required.");
        }
    }

    public class DeleteVaultWorkflowResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}