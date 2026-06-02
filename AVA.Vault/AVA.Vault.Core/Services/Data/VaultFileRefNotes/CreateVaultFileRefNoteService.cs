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
    /// Creates and persists a new VaultFileRefNote link between a VaultFileRef and VaultNote.
    /// </summary>
    public class CreateVaultFileRefNoteService : ApiServiceBase<CreateVaultFileRefNoteRequest, CreateVaultFileRefNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultFileRefNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultFileRefNoteResponse DoWork(CreateVaultFileRefNoteRequest request)
        {
            var response = new CreateVaultFileRefNoteResponse();

            try
            {
                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultFileRefNote>().Any(n => n.ID == request.FileRefNoteID || (n.FileRefID == request.FileRefID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this file reference.";
                    return response;
                }

                var fileRefNote = new VaultFileRefNote
                {
                    ID = string.IsNullOrWhiteSpace(request.FileRefNoteID) ? Guid.NewGuid().ToString() : request.FileRefNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    NoteOrder = request.NoteOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    FileRefID = request.FileRefID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultFileRefNote>().Add(fileRefNote);
                Context.Flush();

                response.FileRefNoteID = fileRefNote.ID;
                response.FileRefNote = fileRefNote;
                response.UserMessage = "Vault file reference note link created successfully.";

                _logger.Log(nameof(CreateVaultFileRefNoteService), $"Created VaultFileRefNote [{fileRefNote.ID}] FileRef [{fileRefNote.FileRefID}] Note [{fileRefNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefNote", fileRefNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultFileRefNoteService), "Error creating VaultFileRefNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault file reference note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultFileRefNoteRequest : CfkAuthorizedApiRequest
    {
        public string? FileRefNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int NoteOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string FileRefID { get; set; }

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
            if (string.IsNullOrWhiteSpace(FileRefID))
                yield return new ValidationResult("FileRefID is required.");

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

    public class CreateVaultFileRefNoteResponse : CfkApiResponse
    {
        public string? FileRefNoteID { get; set; }
        public VaultFileRefNote? FileRefNote { get; set; }
    }

    #endregion
}