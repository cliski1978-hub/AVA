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
    /// Creates and persists a new VaultNoteVaultTag link between a VaultNote and VaultTag.
    /// </summary>
    public class CreateVaultNoteVaultTagService : ApiServiceBase<CreateVaultNoteVaultTagRequest, CreateVaultNoteVaultTagResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultNoteVaultTagService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultNoteVaultTagResponse DoWork(CreateVaultNoteVaultTagRequest request)
        {
            var response = new CreateVaultNoteVaultTagResponse();

            try
            {
                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var tagExists = Context.Set<VaultTag>().Any(t => t.ID == request.TagID);

                if (!tagExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultTag [{request.TagID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultNoteVaultTag>().Any(t => t.ID == request.NoteVaultTagID || (t.NoteID == request.NoteID && t.TagID == request.TagID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This tag is already linked to this note.";
                    return response;
                }

                var noteVaultTag = new VaultNoteVaultTag
                {
                    ID = string.IsNullOrWhiteSpace(request.NoteVaultTagID) ? Guid.NewGuid().ToString() : request.NoteVaultTagID,
                    SortOrder = request.SortOrder,
                    NoteID = request.NoteID,
                    TagID = request.TagID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultNoteVaultTag>().Add(noteVaultTag);
                Context.Flush();

                response.NoteVaultTagID = noteVaultTag.ID;
                response.NoteVaultTag = noteVaultTag;
                response.UserMessage = "Vault note tag link created successfully.";

                _logger.Log(nameof(CreateVaultNoteVaultTagService), $"Created VaultNoteVaultTag [{noteVaultTag.ID}] Note [{noteVaultTag.NoteID}] Tag [{noteVaultTag.TagID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteVaultTag", noteVaultTag.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultNoteVaultTagService), "Error creating VaultNoteVaultTag.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault note tag link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultNoteVaultTagRequest : CfkAuthorizedApiRequest
    {
        public string? NoteVaultTagID { get; set; }

        public int SortOrder { get; set; }

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; }

        [Required]
        [MaxLength(128)]
        public string TagID { get; set; }

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

            if (string.IsNullOrWhiteSpace(TagID))
                yield return new ValidationResult("TagID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultNoteVaultTagResponse : CfkApiResponse
    {
        public string? NoteVaultTagID { get; set; }
        public VaultNoteVaultTag? NoteVaultTag { get; set; }
    }

    #endregion
}