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
    /// Deletes a VaultNoteVaultTag link between a VaultNote and VaultTag.
    /// This does not delete the underlying VaultNote or VaultTag. Orphaned note cleanup will be centralized later if needed.
    /// </summary>
    public class DeleteVaultNoteVaultTagService : ApiServiceBase<DeleteVaultNoteVaultTagRequest, DeleteVaultNoteVaultTagResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultNoteVaultTagService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultNoteVaultTagResponse DoWork(DeleteVaultNoteVaultTagRequest request)
        {
            var response = new DeleteVaultNoteVaultTagResponse();

            try
            {
                var noteVaultTag = Context.Set<VaultNoteVaultTag>().FirstOrDefault(t => t.ID == request.NoteVaultTagID);

                if (noteVaultTag == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault note tag link not found.";
                    response.Deleted = false;
                    return response;
                }

                var noteId = noteVaultTag.NoteID;
                var tagId = noteVaultTag.TagID;

                Context.Set<VaultNoteVaultTag>().Remove(noteVaultTag);
                Context.Flush();

                response.Deleted = true;
                response.NoteVaultTagID = request.NoteVaultTagID;
                response.NoteID = noteId;
                response.TagID = tagId;
                response.UserMessage = "Vault note tag link deleted successfully.";

                _logger.Log(nameof(DeleteVaultNoteVaultTagService), $"Deleted VaultNoteVaultTag [{request.NoteVaultTagID}] Note [{noteId}] Tag [{tagId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteVaultTag", request.NoteVaultTagID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here if needed.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultNoteVaultTagService), "Error deleting VaultNoteVaultTag.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault note tag link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultNoteVaultTagRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteVaultTagID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteVaultTagID))
                yield return new ValidationResult("NoteVaultTagID is required.");
        }
    }

    public class DeleteVaultNoteVaultTagResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? NoteVaultTagID { get; set; }
        public string? NoteID { get; set; }
        public string? TagID { get; set; }
    }

    #endregion
}