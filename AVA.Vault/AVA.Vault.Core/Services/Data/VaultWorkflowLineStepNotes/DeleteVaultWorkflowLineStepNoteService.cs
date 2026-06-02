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
    /// Deletes a VaultWorkflowLineStepNote link between a VaultWorkflowLineStep and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultWorkflowLineStepNoteService : ApiServiceBase<DeleteVaultWorkflowLineStepNoteRequest, DeleteVaultWorkflowLineStepNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultWorkflowLineStepNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultWorkflowLineStepNoteResponse DoWork(DeleteVaultWorkflowLineStepNoteRequest request)
        {
            var response = new DeleteVaultWorkflowLineStepNoteResponse();

            try
            {
                var workflowLineStepNote = Context.Set<VaultWorkflowLineStepNote>().FirstOrDefault(n => n.ID == request.WorkflowLineStepNoteID);

                if (workflowLineStepNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault workflow line step note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var workflowLineStepId = workflowLineStepNote.WorkflowLineStepID;
                var noteId = workflowLineStepNote.NoteID;

                Context.Set<VaultWorkflowLineStepNote>().Remove(workflowLineStepNote);
                Context.Flush();

                response.Deleted = true;
                response.WorkflowLineStepNoteID = request.WorkflowLineStepNoteID;
                response.WorkflowLineStepID = workflowLineStepId;
                response.NoteID = noteId;
                response.UserMessage = "Vault workflow line step note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultWorkflowLineStepNoteService), $"Deleted VaultWorkflowLineStepNote [{request.WorkflowLineStepNoteID}] WorkflowLineStep [{workflowLineStepId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepNote", request.WorkflowLineStepNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultWorkflowLineStepNoteService), "Error deleting VaultWorkflowLineStepNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault workflow line step note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultWorkflowLineStepNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowLineStepNoteID))
                yield return new ValidationResult("WorkflowLineStepNoteID is required.");
        }
    }

    public class DeleteVaultWorkflowLineStepNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? WorkflowLineStepNoteID { get; set; }
        public string? WorkflowLineStepID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}