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
    /// Deletes a VaultWorkflowNodeNote link between a VaultWorkflowNode and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowNodeNoteService : ApiServiceBase<DeleteVaultWorkflowNodeNoteRequest, DeleteVaultWorkflowNodeNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowNodeNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowNodeNoteResponse DoWork(DeleteVaultWorkflowNodeNoteRequest request)
        {
            var response = new DeleteVaultWorkflowNodeNoteResponse();

            try
            {
                var workflowNodeNote = Context.Set<VaultWorkflowNodeNote>().FirstOrDefault(n => n.ID == request.WorkflowNodeNoteID);

                if (workflowNodeNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow node note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowNodeId = workflowNodeNote.WorkflowNodeID;
                var noteId = workflowNodeNote.NoteID;

                Context.Set<VaultWorkflowNodeNote>().Remove(workflowNodeNote);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowNodeNoteID = request.WorkflowNodeNoteID;
                response.WorkflowNodeID = workflowNodeId;
                response.NoteID = noteId;
                response.UserMessage = "Vault workflow node note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowNodeNoteService), $"Deleted VaultWorkflowNodeNote [{request.WorkflowNodeNoteID}] WorkflowNode [{workflowNodeId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeNote", request.WorkflowNodeNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowNodeNoteService), "Error deleting VaultWorkflowNodeNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow node note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowNodeNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNodeNoteID))
                yield return new ValidationResult("WorkflowNodeNoteID is required.");
        }
    }

    public class DeleteVaultWorkflowNodeNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowNodeNoteID { get; set; }
        public string? WorkflowNodeID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}