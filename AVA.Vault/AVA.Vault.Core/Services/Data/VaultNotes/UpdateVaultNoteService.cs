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
    /// Updates an existing VaultNote.
    /// VaultID is the required ownership/root container.
    /// SessionID is optional and should not define normal note uniqueness.
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
                var note = Context.Set<VaultNote>().FirstOrDefault(n => n.ID == request.NoteID);

                if (note == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                    return response;
                }

                var vaultID = string.IsNullOrWhiteSpace(request.VaultID) ? note.VaultID : request.VaultID;
                var sessionID = request.SessionID;

                if (!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != note.VaultID)
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                        return response;
                    }
                }

                if (request.SessionID != null && request.SessionID != note.SessionID)
                {
                    if (!string.IsNullOrWhiteSpace(request.SessionID))
                    {
                        var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                        if (!sessionExists)
                        {
                            response.Code = 404;
                            response.UserMessage = $"VaultSession '{request.SessionID}' not found.";
                            return response;
                        }
                    }
                }
                else
                {
                    sessionID = note.SessionID;
                }

                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    var duplicateTitleExists = Context.Set<VaultNote>().Any(n => n.ID != note.ID && n.VaultID == vaultID && n.Title.ToLower() == request.Title.ToLower());

                    if (duplicateTitleExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A note titled '{request.Title}' already exists in this vault.";
                        return response;
                    }

                    note.Title = request.Title;
                }

                note.VaultID = vaultID;
                note.SessionID = sessionID;

                if (request.Content != null)
                    note.Content = request.Content;

                if (request.EmbeddingJson != null)
                    note.EmbeddingJson = request.EmbeddingJson;

                if (request.IsPinned.HasValue)
                    note.IsPinned = request.IsPinned.Value;

                if (request.IsSynced.HasValue)
                    note.IsSynced = request.IsSynced.Value;

                if (request.IsTemplate.HasValue)
                    note.IsTemplate = request.IsTemplate.Value;

                if (request.MetadataJson != null)
                    note.MetadataJson = request.MetadataJson;

                if (request.SortOrder.HasValue)
                    note.SortOrder = request.SortOrder.Value;

                if (request.TemplateName != null)
                    note.TemplateName = request.TemplateName;

                if (request.Summary != null)
                    note.Summary = request.Summary;

                if (request.PrimaryIdentityId != null)
                    note.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    note.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    note.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    note.IdentityList = request.IdentityList;

                note.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                response.NoteID = note.ID;
                response.Note = note;
                response.UserMessage = "Vault note updated successfully.";

                _logger.Log(nameof(UpdateVaultNoteService), $"Updated VaultNote [{note.ID}] '{note.Title}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultNoteService), "Error updating VaultNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault note.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteID { get; set; }

        public string? Content { get; set; }

        public string? EmbeddingJson { get; set; }

        public bool? IsPinned { get; set; }

        public bool? IsSynced { get; set; }

        public bool? IsTemplate { get; set; }

        public string? MetadataJson { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(256)]
        public string? TemplateName { get; set; }

        [MaxLength(512)]
        public string? Summary { get; set; }

        [MaxLength(256)]
        public string? Title { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultNoteResponse : CfkApiResponse
    {
        public string? NoteID { get; set; }
        public VaultNote? Note { get; set; }
    }

    #endregion
}