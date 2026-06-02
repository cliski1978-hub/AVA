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
    /// Deletes a VaultProject and project-owned/link records.
    /// This does not delete standalone VaultNote content directly. Note cleanup will be centralized later.
    /// FileRef cleanup may need to be revisited when centralized file orphan cleanup is wired in.
    /// </summary>
    public class DeleteVaultProjectService : ApiServiceBase<DeleteVaultProjectRequest, DeleteVaultProjectResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultProjectService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultProjectResponse DoWork(DeleteVaultProjectRequest request)
        {
            var response = new DeleteVaultProjectResponse();

            try
            {
                var project = Context.Set<VaultProject>().FirstOrDefault(p => p.ID == request.ProjectID);

                if (project == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault project not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflows = Context.Set<VaultWorkflow>().Where(w => w.ProjectID == project.ID).ToList();
                var workflowIds = workflows.Select(w => w.ID).ToList();

                var workflowNodes = Context.Set<VaultWorkflowNode>().Where(n => workflowIds.Contains(n.WorkflowID)).ToList();
                var workflowNodeIds = workflowNodes.Select(n => n.ID).ToList();

                var workflowLines = Context.Set<VaultWorkflowLine>().Where(l => workflowIds.Contains(l.WorkflowID) || workflowNodeIds.Contains(l.SourceWorkflowNodeID) || workflowNodeIds.Contains(l.TargetWorkflowNodeID)).ToList();
                var workflowLineIds = workflowLines.Select(l => l.ID).ToList();

                var workflowLineSteps = Context.Set<VaultWorkflowLineStep>().Where(s => workflowLineIds.Contains(s.WorkflowLineID)).ToList();
                var workflowLineStepIds = workflowLineSteps.Select(s => s.ID).ToList();

                var workflowNotes = Context.Set<VaultWorkflowNote>().Where(n => workflowIds.Contains(n.WorkflowID)).ToList();
                var workflowFileRefs = Context.Set<VaultWorkflowFileRef>().Where(f => workflowIds.Contains(f.WorkflowID)).ToList();

                var workflowNodeNotes = Context.Set<VaultWorkflowNodeNote>().Where(n => workflowNodeIds.Contains(n.WorkflowNodeID)).ToList();
                var workflowNodeFileRefs = Context.Set<VaultWorkflowNodeFileRef>().Where(f => workflowNodeIds.Contains(f.WorkflowNodeID)).ToList();

                var workflowLineNotes = Context.Set<VaultWorkflowLineNote>().Where(n => workflowLineIds.Contains(n.WorkflowLineID)).ToList();
                var workflowLineFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => workflowLineIds.Contains(f.WorkflowLineID)).ToList();

                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => workflowLineStepIds.Contains(n.WorkflowLineStepID)).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => workflowLineStepIds.Contains(f.WorkflowLineStepID)).ToList();

                var sessions = Context.Set<VaultSession>().Where(s => s.ProjectID == project.ID).ToList();
                var sessionIds = sessions.Select(s => s.ID).ToList();

                var sessionNotes = Context.Set<VaultSessionNote>().Where(n => sessionIds.Contains(n.SessionID)).ToList();
                var sessionFileRefs = Context.Set<VaultSessionFileRef>().Where(f => sessionIds.Contains(f.SessionID)).ToList();

                var graphs = Context.Set<VaultGraph>().Where(g => g.ProjectID == project.ID).ToList();

                var projectNotes = Context.Set<VaultProjectNote>().Where(n => n.ProjectID == project.ID).ToList();
                var projectFileRefs = Context.Set<VaultProjectFileRef>().Where(f => f.ProjectID == project.ID).ToList();

                var tags = Context.Set<VaultTag>().Where(t => t.ProjectID == project.ID).ToList();
                var tagIds = tags.Select(t => t.ID).ToList();
                var noteVaultTags = Context.Set<VaultNoteVaultTag>().Where(t => tagIds.Contains(t.TagID)).ToList();

                var directFileRefs = Context.Set<VaultFileRef>().Where(f => f.ProjectID == project.ID).ToList();
                var directFileRefIds = directFileRefs.Select(f => f.ID).ToList();

                var fileRefRelations = Context.Set<VaultFileRefRelation>().Where(r => directFileRefIds.Contains(r.SourceFileRefID) || directFileRefIds.Contains(r.TargetFileRefID)).ToList();
                var fileRefNotes = Context.Set<VaultFileRefNote>().Where(n => directFileRefIds.Contains(n.FileRefID)).ToList();
                var headerFileRefs = Context.Set<VaultHeaderFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();
                var noteFileRefs = Context.Set<VaultNoteFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();
                var workflowDirectFileRefs = Context.Set<VaultWorkflowFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();
                var workflowNodeDirectFileRefs = Context.Set<VaultWorkflowNodeFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();
                var workflowLineDirectFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();
                var workflowLineStepDirectFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => directFileRefIds.Contains(f.FileRefID)).ToList();

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

                foreach (var item in workflows)
                    Context.Set<VaultWorkflow>().Remove(item);

                foreach (var item in sessionNotes)
                    Context.Set<VaultSessionNote>().Remove(item);

                foreach (var item in sessionFileRefs)
                    Context.Set<VaultSessionFileRef>().Remove(item);

                foreach (var item in sessions)
                    Context.Set<VaultSession>().Remove(item);

                foreach (var item in graphs)
                    Context.Set<VaultGraph>().Remove(item);

                foreach (var item in projectNotes)
                    Context.Set<VaultProjectNote>().Remove(item);

                foreach (var item in projectFileRefs)
                    Context.Set<VaultProjectFileRef>().Remove(item);

                foreach (var item in noteVaultTags)
                    Context.Set<VaultNoteVaultTag>().Remove(item);

                foreach (var item in tags)
                    Context.Set<VaultTag>().Remove(item);

                foreach (var item in fileRefRelations)
                    Context.Set<VaultFileRefRelation>().Remove(item);

                foreach (var item in fileRefNotes)
                    Context.Set<VaultFileRefNote>().Remove(item);

                foreach (var item in headerFileRefs)
                    Context.Set<VaultHeaderFileRef>().Remove(item);

                foreach (var item in noteFileRefs)
                    Context.Set<VaultNoteFileRef>().Remove(item);

                foreach (var item in workflowDirectFileRefs)
                    Context.Set<VaultWorkflowFileRef>().Remove(item);

                foreach (var item in workflowNodeDirectFileRefs)
                    Context.Set<VaultWorkflowNodeFileRef>().Remove(item);

                foreach (var item in workflowLineDirectFileRefs)
                    Context.Set<VaultWorkflowLineFileRef>().Remove(item);

                foreach (var item in workflowLineStepDirectFileRefs)
                    Context.Set<VaultWorkflowLineStepFileRef>().Remove(item);

                foreach (var item in directFileRefs)
                    Context.Set<VaultFileRef>().Remove(item);

                Context.Set<VaultProject>().Remove(project);
                Context.Flush();

                response.Deleted = true;
                response.ProjectID = request.ProjectID;
                response.UserMessage = "Vault project and related project data deleted successfully.";

                _logger.Log(nameof(DeleteVaultProjectService), $"Deleted VaultProject [{request.ProjectID}] '{project.Name}' and related project records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", request.ProjectID, "Deleted");

                // TODO: Revisit this delete after DeleteVaultNoteService and centralized orphan cleanup services are wired in.
                // TODO: Revisit direct VaultFileRef deletion after centralized file orphan cleanup is wired in.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultProjectService), "Error deleting VaultProject.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault project.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultProjectRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectID { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
        }
    }

    public class DeleteVaultProjectResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? ProjectID { get; set; }
    }

    #endregion
}