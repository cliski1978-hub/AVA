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
    /// Deletes a VaultNoteFileRef link between a VaultNote and VaultFileRef.
    /// This does not delete the underlying VaultNote or VaultFileRef. Orphan cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultNoteFileRefService : ApiServiceBase<DeleteVaultNoteFileRefRequest, DeleteVaultNoteFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultNoteFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultNoteFileRefResponse DoWork(DeleteVaultNoteFileRefRequest request)
        {
            var response = new DeleteVaultNoteFileRefResponse();

            try
            {
                var noteFileRef = Context.Set<VaultNoteFileRef>().FirstOrDefault(f => f.ID == request.NoteFileRefID);

                if (noteFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault note file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var noteId = noteFileRef.NoteID;
                var fileRefId = noteFileRef.FileRefID;

                Context.Set<VaultNoteFileRef>().Remove(noteFileRef);
                Context.Flush();

                response.Deleted = true;
                response.NoteFileRefID = request.NoteFileRefID;
                response.NoteID = noteId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault note file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultNoteFileRefService), $"Deleted VaultNoteFileRef [{request.NoteFileRefID}] Note [{noteId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteFileRef", request.NoteFileRefID, "Deleted");

                // TODO: After note/file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultNoteFileRefService), "Error deleting VaultNoteFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault note file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultNoteFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteFileRefID))
                yield return new ValidationResult("NoteFileRefID is required.");
        }
    }

    public class DeleteVaultNoteFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? NoteFileRefID { get; set; }
        public string? NoteID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}