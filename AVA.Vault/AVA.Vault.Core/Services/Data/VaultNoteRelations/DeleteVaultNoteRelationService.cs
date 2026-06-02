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
    /// Deletes a VaultNoteRelation between two VaultNotes.
    /// This does not delete either underlying VaultNote. Orphaned note cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultNoteRelationService : ApiServiceBase<DeleteVaultNoteRelationRequest, DeleteVaultNoteRelationResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultNoteRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultNoteRelationResponse DoWork(DeleteVaultNoteRelationRequest request)
        {
            var response = new DeleteVaultNoteRelationResponse();

            try
            {
                var noteRelation = Context.Set<VaultNoteRelation>().FirstOrDefault(r => r.ID == request.NoteRelationID);

                if (noteRelation == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault note relation not found.";
                    response.Deleted = false;
                    return response;
                }

                var sourceNoteId = noteRelation.SourceNoteID;
                var targetNoteId = noteRelation.TargetNoteID;

                Context.Set<VaultNoteRelation>().Remove(noteRelation);
                Context.Flush();

                response.Deleted = true;
                response.NoteRelationID = request.NoteRelationID;
                response.SourceNoteID = sourceNoteId;
                response.TargetNoteID = targetNoteId;
                response.UserMessage = "Vault note relation deleted successfully.";

                _logger.Log(nameof(DeleteVaultNoteRelationService), $"Deleted VaultNoteRelation [{request.NoteRelationID}] SourceNote [{sourceNoteId}] TargetNote [{targetNoteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", request.NoteRelationID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here if needed.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultNoteRelationService), "Error deleting VaultNoteRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault note relation.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultNoteRelationRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteRelationID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteRelationID))
                yield return new ValidationResult("NoteRelationID is required.");
        }
    }

    public class DeleteVaultNoteRelationResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? NoteRelationID { get; set; }
        public string? SourceNoteID { get; set; }
        public string? TargetNoteID { get; set; }
    }

    #endregion
}