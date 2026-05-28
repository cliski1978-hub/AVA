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
    /// Assigns an existing VaultTag to a VaultNote (many-to-many).
    /// No-ops if the tag is already assigned to the note.
    /// </summary>
    public class AssignTagToNoteService : ApiServiceBase<AssignTagToNoteRequest, AssignTagToNoteResponse>
    {
        private readonly VaultLogger _logger;

        public AssignTagToNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override AssignTagToNoteResponse DoWork(AssignTagToNoteRequest request)
        {
            var response = new AssignTagToNoteResponse();

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

                var tag = Context.Set<VaultTag>()
                    .FirstOrDefault(t => t.ID == request.TagID && t.ProjectID == request.VaultID);

                if (tag == null)
                {
                    response.UserMessage = $"Tag '{request.TagID}' not found in vault '{request.VaultID}'.";
                    return response;
                }

                if (note.VaultNoteVaultTags.Any(jt => jt.TagID == tag.ID))
                {
                    response.UserMessage = "Tag already assigned to this note.";
                    response.Assigned = true;
                    return response;
                }

                note.VaultNoteVaultTags.Add(new VaultNoteVaultTag
                {
                    ID = Guid.NewGuid().ToString(),
                    NoteID = note.ID,
                    TagID = tag.ID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                note.UpdatedAt = DateTime.UtcNow;
                Context.Flush();

                _logger.Log(nameof(AssignTagToNoteService),
                    $"Assigned tag [{tag.ID}] '{tag.Name}' to note [{note.ID}]");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "TagAssigned");

                response.Assigned    = true;
                response.UserMessage = "Tag assigned successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(AssignTagToNoteService), "Error assigning tag to note.", ex);
                response.UserMessage = "An error occurred while assigning the tag.";
            }

            return response;
        }
    }

    #region Models

    public class AssignTagToNoteRequest : CfkAuthorizedApiRequest
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

    public class AssignTagToNoteResponse : CfkApiResponse
    {
        public bool Assigned { get; set; }
    }

    #endregion
}
