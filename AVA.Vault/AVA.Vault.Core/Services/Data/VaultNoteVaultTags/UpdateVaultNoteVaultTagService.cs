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
    /// Updates an existing VaultNoteVaultTag link between a VaultNote and VaultTag.
    /// This does not update the underlying VaultNote or VaultTag.
    /// </summary>
    public class UpdateVaultNoteVaultTagService : ApiServiceBase<UpdateVaultNoteVaultTagRequest, UpdateVaultNoteVaultTagResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultNoteVaultTagService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultNoteVaultTagResponse DoWork(UpdateVaultNoteVaultTagRequest request)
        {
            var response = new UpdateVaultNoteVaultTagResponse();

            try
            {
                var noteVaultTag = Context.Set<VaultNoteVaultTag>().FirstOrDefault(t => t.ID == request.NoteVaultTagID);

                if (noteVaultTag == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNoteVaultTag '{request.NoteVaultTagID}' not found.";
                    return response;
                }

                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? noteVaultTag.NoteID : request.NoteID;
                var tagID = string.IsNullOrWhiteSpace(request.TagID) ? noteVaultTag.TagID : request.TagID;

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != noteVaultTag.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.TagID) && request.TagID != noteVaultTag.TagID)
                {
                    var tagExists = Context.Set<VaultTag>().Any(t => t.ID == request.TagID);

                    if (!tagExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultTag '{request.TagID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != noteVaultTag.NoteID) || (!string.IsNullOrWhiteSpace(request.TagID) && request.TagID != noteVaultTag.TagID))
                {
                    var duplicateExists = Context.Set<VaultNoteVaultTag>().Any(t => t.ID != noteVaultTag.ID && t.NoteID == noteID && t.TagID == tagID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This tag is already linked to this note.";
                        return response;
                    }
                }

                noteVaultTag.NoteID = noteID;
                noteVaultTag.TagID = tagID;

                if (request.SortOrder.HasValue)
                    noteVaultTag.SortOrder = request.SortOrder.Value;

                if (request.PrimaryIdentityId != null)
                    noteVaultTag.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    noteVaultTag.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    noteVaultTag.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    noteVaultTag.IdentityList = request.IdentityList;

                noteVaultTag.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultNoteVaultTagService), $"Updated VaultNoteVaultTag [{noteVaultTag.ID}] Note [{noteVaultTag.NoteID}] Tag [{noteVaultTag.TagID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteVaultTag", noteVaultTag.ID, "Updated");

                response.NoteVaultTagID = noteVaultTag.ID;
                response.NoteVaultTag = noteVaultTag;
                response.UserMessage = "Vault note tag link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultNoteVaultTagService), "Error updating VaultNoteVaultTag.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault note tag link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultNoteVaultTagRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteVaultTagID { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(128)]
        public string? NoteID { get; set; }

        [MaxLength(128)]
        public string? TagID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteVaultTagID))
                yield return new ValidationResult("NoteVaultTagID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultNoteVaultTagResponse : CfkApiResponse
    {
        public string? NoteVaultTagID { get; set; }
        public VaultNoteVaultTag? NoteVaultTag { get; set; }
    }

    #endregion
}