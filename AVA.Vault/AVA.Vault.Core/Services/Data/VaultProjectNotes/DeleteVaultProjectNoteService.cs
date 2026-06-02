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
    /// Deletes a VaultProjectNote link between a VaultProject and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultProjectNoteService : ApiServiceBase<DeleteVaultProjectNoteRequest, DeleteVaultProjectNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultProjectNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultProjectNoteResponse DoWork(DeleteVaultProjectNoteRequest request)
        {
            var response = new DeleteVaultProjectNoteResponse();

            try
            {
                var projectNote = Context.Set<VaultProjectNote>().FirstOrDefault(n => n.ID == request.ProjectNoteID);

                if (projectNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault project note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var projectId = projectNote.ProjectID;
                var noteId = projectNote.NoteID;

                Context.Set<VaultProjectNote>().Remove(projectNote);
                Context.Flush();

                response.Deleted = true;
                response.ProjectNoteID = request.ProjectNoteID;
                response.ProjectID = projectId;
                response.NoteID = noteId;
                response.UserMessage = "Vault project note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultProjectNoteService), $"Deleted VaultProjectNote [{request.ProjectNoteID}] Project [{projectId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectNote", request.ProjectNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultProjectNoteService), "Error deleting VaultProjectNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault project note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultProjectNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectNoteID))
                yield return new ValidationResult("ProjectNoteID is required.");
        }
    }

    public class DeleteVaultProjectNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? ProjectNoteID { get; set; }
        public string? ProjectID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}