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
    /// Deletes a VaultSessionNote link between a VaultSession and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultSessionNoteService : ApiServiceBase<DeleteVaultSessionNoteRequest, DeleteVaultSessionNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultSessionNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultSessionNoteResponse DoWork(DeleteVaultSessionNoteRequest request)
        {
            var response = new DeleteVaultSessionNoteResponse();

            try
            {
                var sessionNote = Context.Set<VaultSessionNote>().FirstOrDefault(n => n.ID == request.SessionNoteID);

                if (sessionNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault session note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var sessionId = sessionNote.SessionID;
                var noteId = sessionNote.NoteID;

                Context.Set<VaultSessionNote>().Remove(sessionNote);
                Context.Flush();

                response.Deleted = true;
                response.SessionNoteID = request.SessionNoteID;
                response.SessionID = sessionId;
                response.NoteID = noteId;
                response.UserMessage = "Vault session note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultSessionNoteService), $"Deleted VaultSessionNote [{request.SessionNoteID}] Session [{sessionId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionNote", request.SessionNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultSessionNoteService), "Error deleting VaultSessionNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault session note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultSessionNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionNoteID))
                yield return new ValidationResult("SessionNoteID is required.");
        }
    }

    public class DeleteVaultSessionNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? SessionNoteID { get; set; }
        public string? SessionID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}