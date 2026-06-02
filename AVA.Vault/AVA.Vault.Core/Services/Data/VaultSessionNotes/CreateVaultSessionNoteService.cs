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
    /// Creates and persists a new VaultSessionNote link between a VaultSession and VaultNote.
    /// </summary>
    public class CreateVaultSessionNoteService : ApiServiceBase<CreateVaultSessionNoteRequest, CreateVaultSessionNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultSessionNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultSessionNoteResponse DoWork(CreateVaultSessionNoteRequest request)
        {
            var response = new CreateVaultSessionNoteResponse();

            try
            {
                var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                if (!sessionExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultSession [{request.SessionID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultSessionNote>().Any(n => n.ID == request.SessionNoteID || (n.SessionID == request.SessionID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this session.";
                    return response;
                }

                var sessionNote = new VaultSessionNote
                {
                    ID = string.IsNullOrWhiteSpace(request.SessionNoteID) ? Guid.NewGuid().ToString() : request.SessionNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    SessionID = request.SessionID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultSessionNote>().Add(sessionNote);
                Context.Flush();

                response.SessionNoteID = sessionNote.ID;
                response.SessionNote = sessionNote;
                response.UserMessage = "Vault session note link created successfully.";

                _logger.Log(nameof(CreateVaultSessionNoteService), $"Created VaultSessionNote [{sessionNote.ID}] Session [{sessionNote.SessionID}] Note [{sessionNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionNote", sessionNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultSessionNoteService), "Error creating VaultSessionNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault session note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultSessionNoteRequest : CfkAuthorizedApiRequest
    {
        public string? SessionNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string SessionID { get; set; }

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionID))
                yield return new ValidationResult("SessionID is required.");

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

    public class CreateVaultSessionNoteResponse : CfkApiResponse
    {
        public string? SessionNoteID { get; set; }
        public VaultSessionNote? SessionNote { get; set; }
    }

    #endregion
}