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
    /// Creates and persists a new VaultNoteFileRef link between a VaultNote and VaultFileRef.
    /// </summary>
    public class CreateVaultNoteFileRefService : ApiServiceBase<CreateVaultNoteFileRefRequest, CreateVaultNoteFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultNoteFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultNoteFileRefResponse DoWork(CreateVaultNoteFileRefRequest request)
        {
            var response = new CreateVaultNoteFileRefResponse();

            try
            {
                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultNoteFileRef>().Any(f => f.ID == request.NoteFileRefID || (f.NoteID == request.NoteID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this note.";
                    return response;
                }

                var noteFileRef = new VaultNoteFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.NoteFileRefID) ? Guid.NewGuid().ToString() : request.NoteFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    NoteID = request.NoteID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultNoteFileRef>().Add(noteFileRef);
                Context.Flush();

                response.NoteFileRefID = noteFileRef.ID;
                response.NoteFileRef = noteFileRef;
                response.UserMessage = "Vault note file reference link created successfully.";

                _logger.Log(nameof(CreateVaultNoteFileRefService), $"Created VaultNoteFileRef [{noteFileRef.ID}] Note [{noteFileRef.NoteID}] FileRef [{noteFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteFileRef", noteFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultNoteFileRefService), "Error creating VaultNoteFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault note file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultNoteFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? NoteFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; }

        [Required]
        [MaxLength(128)]
        public string FileRefID { get; set; }

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

            if (string.IsNullOrWhiteSpace(FileRefID))
                yield return new ValidationResult("FileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultNoteFileRefResponse : CfkApiResponse
    {
        public string? NoteFileRefID { get; set; }
        public VaultNoteFileRef? NoteFileRef { get; set; }
    }

    #endregion
}