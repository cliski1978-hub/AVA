using System.ComponentModel.DataAnnotations;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;


namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Deletes a VaultNote and cascades related links and metadata.
    /// </summary>
    public class DeleteVaultNoteService : ApiServiceBase<DeleteVaultNoteRequest, DeleteVaultNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultNoteResponse DoWork(DeleteVaultNoteRequest request)
        {
            var response = new DeleteVaultNoteResponse();

            try
            {
                var note = Context.Set<VaultNote>()
                    .FirstOrDefault(n => n.ID == request.NoteID && n.VaultID == request.VaultID);

                if (note == null)
                {
                    response.UserMessage = "Vault note not found.";
                    response.Deleted = false;
                    return response;
                }

                // Remove related data (relations, metadata)
                var relations = Context.Set<VaultNoteRelation>().Where(r => r.SourceNoteID == note.ID || r.TargetNoteID == note.ID).ToList();
                foreach (var relation in relations) Context.Set<VaultNoteRelation>().Remove(relation);

                var metadata = Context.Set<VaultMetadata>().Where(m => m.NoteID == note.ID).ToList();
                foreach (var meta in metadata) Context.Set<VaultMetadata>().Remove(meta);

                Context.Set<VaultNote>().Remove(note);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Deleted");
                Context.Flush();

                _logger.Log(nameof(DeleteVaultNoteService),
                    $"Deleted VaultNote [{note.ID}] '{note.Title}' from Vault {note.VaultID} with {relations.Count} relations and {metadata.Count} metadata entries.");

                response.Deleted = true;
                response.UserMessage = "Vault note and related data deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultNoteService), "Error deleting VaultNote.", ex);
                response.UserMessage = "An error occurred while deleting the VaultNote.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");
        }
    }

    public class DeleteVaultNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
