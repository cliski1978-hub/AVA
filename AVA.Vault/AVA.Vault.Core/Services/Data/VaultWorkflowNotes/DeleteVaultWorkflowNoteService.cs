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
    /// Deletes a VaultWorkflowNote link between a VaultWorkflow and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowNoteService : ApiServiceBase<DeleteVaultWorkflowNoteRequest, DeleteVaultWorkflowNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowNoteResponse DoWork(DeleteVaultWorkflowNoteRequest request)
        {
            var response = new DeleteVaultWorkflowNoteResponse();

            try
            {
                var workflowNote = Context.Set<VaultWorkflowNote>().FirstOrDefault(n => n.ID == request.WorkflowNoteID);

                if (workflowNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var noteId = workflowNote.NoteID;
                var workflowId = workflowNote.WorkflowID;

                Context.Set<VaultWorkflowNote>().Remove(workflowNote);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowNoteID = request.WorkflowNoteID;
                response.WorkflowID = workflowId;
                response.NoteID = noteId;
                response.UserMessage = "Vault workflow note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowNoteService), $"Deleted VaultWorkflowNote [{request.WorkflowNoteID}] Workflow [{workflowId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNote", request.WorkflowNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
                // Example:
                // _cleanupVaultNoteIfOrphanedService.Execute(new CleanupVaultNoteIfOrphanedRequest
                // {
                //     NoteID = noteId,
                //     RequestPartyName = request.RequestPartyName
                // });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowNoteService), "Error deleting VaultWorkflowNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultWorkflowNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNoteID))
                yield return new ValidationResult("WorkflowNoteID is required.");
        }
    }

    public class DeleteVaultWorkflowNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowNoteID { get; set; }
        public string? WorkflowID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}