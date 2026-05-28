using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Removes a VaultTag from a VaultNote (many-to-many).
    /// No-ops if the tag is not assigned to the note.
    /// Does NOT delete the tag itself — use DeleteVaultTagService for that.
    /// </summary>
    public class RemoveTagFromNoteService : ApiServiceBase<RemoveTagFromNoteRequest, RemoveTagFromNoteResponse>
    {
        private readonly VaultLogger _logger;

        public RemoveTagFromNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override RemoveTagFromNoteResponse DoWork(RemoveTagFromNoteRequest request)
        {
            var response = new RemoveTagFromNoteResponse();

            try
            {
                var note = Context.Set<VaultNote>()
                    .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                    .FirstOrDefault(n => n.ID == request.NoteID && n.VaultID == request.VaultID);

                if (note == null)
                {
                    response.UserMessage = $"Note '{request.NoteID}' not found.";
                    return response;
                }

                var jt = note.VaultNoteVaultTags.FirstOrDefault(j => j.TagID == request.TagID);

                if (jt == null)
                {
                    response.UserMessage = "Tag not assigned to this note.";
                    response.Removed = true;
                    return response;
                }

                note.VaultNoteVaultTags.Remove(jt);
                note.UpdatedAt = DateTime.UtcNow;
                Context.Flush();

                _logger.Log(nameof(RemoveTagFromNoteService),
                    $"Removed tag [{jt.TagID}] from note [{note.ID}]");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "TagRemoved");

                response.Removed     = true;
                response.UserMessage = "Tag removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(RemoveTagFromNoteService), "Error removing tag from note.", ex);
                response.UserMessage = "An error occurred while removing the tag.";
            }

            return response;
        }
    }

    #region Models

    public class RemoveTagFromNoteRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID { get; set; }
        [Required] public string TagID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID)) yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))  yield return new ValidationResult("NoteID is required.");
            if (string.IsNullOrWhiteSpace(TagID))   yield return new ValidationResult("TagID is required.");
        }
    }

    public class RemoveTagFromNoteResponse : CfkApiResponse
    {
        public bool Removed { get; set; }
    }

    #endregion
}
