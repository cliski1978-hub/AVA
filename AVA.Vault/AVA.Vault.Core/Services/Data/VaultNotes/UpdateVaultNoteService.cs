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
    /// Updates an existing VaultNote�s content, title, or metadata.
    /// </summary>
    public class UpdateVaultNoteService : ApiServiceBase<UpdateVaultNoteRequest, UpdateVaultNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultNoteResponse DoWork(UpdateVaultNoteRequest request)
        {
            var response = new UpdateVaultNoteResponse();

            try
            {
                var note = Context.Set<VaultNote>()
                    .FirstOrDefault(n => n.ID == request.NoteID && n.VaultID == request.VaultID);

                if (note == null)
                {
                    response.UserMessage = "Vault note not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.Title))
                    note.Title = request.Title;

                if (request.Content != null)
                    note.Content = request.Content;

                if (request.MetadataJson != null)
                    note.MetadataJson = request.MetadataJson;

                if (request.EmbeddingJson != null)
                    note.EmbeddingJson = request.EmbeddingJson;

                note.UpdatedAt = DateTime.UtcNow;
                note.IsSynced = false;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultNoteService),
                    $"Updated VaultNote [{note.ID}] '{note.Title}' in Vault {note.VaultID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Updated");

                response.NoteID = note.ID;
                response.UserMessage = "Vault note updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultNoteService), "Error updating VaultNote.", ex);
                response.UserMessage = "An error occurred while updating the VaultNote.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID { get; set; }
        [MaxLength(256)] public string? Title { get; set; }
        public string? Content { get; set; }
        public string? MetadataJson { get; set; }
        public string? EmbeddingJson { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");
        }
    }

    public class UpdateVaultNoteResponse : CfkApiResponse
    {
        public string? NoteID { get; set; }
    }

    #endregion
}
