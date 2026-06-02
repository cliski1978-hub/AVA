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
    /// Updates an existing VaultSessionNote link between a VaultSession and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultSessionNoteService : ApiServiceBase<UpdateVaultSessionNoteRequest, UpdateVaultSessionNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultSessionNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultSessionNoteResponse DoWork(UpdateVaultSessionNoteRequest request)
        {
            var response = new UpdateVaultSessionNoteResponse();

            try
            {
                var sessionNote = Context.Set<VaultSessionNote>().FirstOrDefault(n => n.ID == request.SessionNoteID);

                if (sessionNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultSessionNote '{request.SessionNoteID}' not found.";
                    return response;
                }

                var sessionID = string.IsNullOrWhiteSpace(request.SessionID) ? sessionNote.SessionID : request.SessionID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? sessionNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.SessionID) && request.SessionID != sessionNote.SessionID)
                {
                    var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                    if (!sessionExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultSession '{request.SessionID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != sessionNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.SessionID) && request.SessionID != sessionNote.SessionID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != sessionNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultSessionNote>().Any(n => n.ID != sessionNote.ID && n.SessionID == sessionID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this session.";
                        return response;
                    }
                }

                sessionNote.SessionID = sessionID;
                sessionNote.NoteID = noteID;

                if (request.Instructions != null)
                    sessionNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    sessionNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    sessionNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    sessionNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    sessionNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    sessionNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    sessionNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    sessionNote.IdentityList = request.IdentityList;

                sessionNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultSessionNoteService), $"Updated VaultSessionNote [{sessionNote.ID}] Session [{sessionNote.SessionID}] Note [{sessionNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionNote", sessionNote.ID, "Updated");

                response.SessionNoteID = sessionNote.ID;
                response.SessionNote = sessionNote;
                response.UserMessage = "Vault session note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultSessionNoteService), "Error updating VaultSessionNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault session note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultSessionNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

        [MaxLength(128)]
        public string? NoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionNoteID))
                yield return new ValidationResult("SessionNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultSessionNoteResponse : CfkApiResponse
    {
        public string? SessionNoteID { get; set; }
        public VaultSessionNote? SessionNote { get; set; }
    }

    #endregion
}