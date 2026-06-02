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
    /// Deletes a VaultWorkflowLineNote link between a VaultWorkflowLine and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowLineNoteService : ApiServiceBase<DeleteVaultWorkflowLineNoteRequest, DeleteVaultWorkflowLineNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineNoteResponse DoWork(DeleteVaultWorkflowLineNoteRequest request)
        {
            var response = new DeleteVaultWorkflowLineNoteResponse();

            try
            {
                var workflowLineNote = Context.Set<VaultWorkflowLineNote>().FirstOrDefault(n => n.ID == request.WorkflowLineNoteID);

                if (workflowLineNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineId = workflowLineNote.WorkflowLineID;
                var noteId = workflowLineNote.NoteID;

                Context.Set<VaultWorkflowLineNote>().Remove(workflowLineNote);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowLineNoteID = request.WorkflowLineNoteID;
                response.WorkflowLineID = workflowLineId;
                response.NoteID = noteId;
                response.UserMessage = "Vault workflow line note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineNoteService), $"Deleted VaultWorkflowLineNote [{request.WorkflowLineNoteID}] WorkflowLine [{workflowLineId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineNote", request.WorkflowLineNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineNoteService), "Error deleting VaultWorkflowLineNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowLineNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineNoteID))
                yield return new ValidationResult("WorkflowLineNoteID is required.");
        }
    }

    public class DeleteVaultWorkflowLineNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowLineNoteID { get; set; }
        public string? WorkflowLineID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}