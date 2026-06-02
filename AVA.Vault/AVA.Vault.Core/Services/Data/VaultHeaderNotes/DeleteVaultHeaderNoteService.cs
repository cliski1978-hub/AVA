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
    /// Deletes a VaultHeaderNote link between a VaultHeader and VaultNote.
    /// This does not delete the underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultHeaderNoteService : ApiServiceBase<DeleteVaultHeaderNoteRequest, DeleteVaultHeaderNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultHeaderNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultHeaderNoteResponse DoWork(DeleteVaultHeaderNoteRequest request)
        {
            var response = new DeleteVaultHeaderNoteResponse();

            try
            {
                var headerNote = Context.Set<VaultHeaderNote>().FirstOrDefault(n => n.ID == request.HeaderNoteID);

                if (headerNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault header note link not found.";
                    response.Deleted = false;
                    return response;
                }

                var vaultId = headerNote.VaultID;
                var noteId = headerNote.NoteID;

                Context.Set<VaultHeaderNote>().Remove(headerNote);
                Context.Flush();

                response.Deleted = true;
                response.HeaderNoteID = request.HeaderNoteID;
                response.VaultID = vaultId;
                response.NoteID = noteId;
                response.UserMessage = "Vault header note link deleted successfully.";

                _logger.Log(nameof(DeleteVaultHeaderNoteService), $"Deleted VaultHeaderNote [{request.HeaderNoteID}] Vault [{vaultId}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderNote", request.HeaderNoteID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultHeaderNoteService), "Error deleting VaultHeaderNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault header note link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultHeaderNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string HeaderNoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(HeaderNoteID))
                yield return new ValidationResult("HeaderNoteID is required.");
        }
    }

    public class DeleteVaultHeaderNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? HeaderNoteID { get; set; }
        public string? VaultID { get; set; }
        public string? NoteID { get; set; }
    }

    #endregion
}