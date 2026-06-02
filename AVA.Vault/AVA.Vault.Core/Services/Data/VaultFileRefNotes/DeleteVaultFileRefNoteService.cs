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
    /// Deletes a VaultFileRefNote link between a VaultFileRef and VaultNote.
    /// This does not delete the underlying VaultNote or VaultFileRef. Orphan cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultFileRefNoteService : ApiServiceBase<DeleteVaultFileRefNoteRequest, DeleteVaultFileRefNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultFileRefNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultFileRefNoteResponse DoWork(DeleteVaultFileRefNoteRequest request)
        {
            var response = new DeleteVaultFileRefNoteResponse();

            try
            {
                var fileRefNote = Context.Set<VaultFileRefNote>().FirstOrDefault(n => n.ID == request.FileRefNoteID);

                if (fileRefNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault file reference note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var fileRefId = fileRefNote.FileRefID;
                var noteId = fileRefNote.NoteID;

                Context.Set<VaultFileRefNote>().Remove(fileRefNote);
                Context.Flush();

                response.Deleted = true;
                response.FileRefNoteID = request.FileRefNoteID;
                response.FileRefID = fileRefId;
                response.NoteID = noteId;
                response.UserMessage = "Vault file reference note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultFileRefNoteService), $"Deleted VaultFileRefNote [{request.FileRefNoteID}] FileRef [{fileRefId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefNote", request.FileRefNoteID, "Deleted");

                // TODO: After note/file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultFileRefNoteService), "Error deleting VaultFileRefNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault file reference note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultFileRefNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileRefNoteID))
                yield return new ValidationResult("FileRefNoteID is required.");
        }
    }

    public class DeleteVaultFileRefNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? FileRefNoteID { get; set; }
        public string? FileRefID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}