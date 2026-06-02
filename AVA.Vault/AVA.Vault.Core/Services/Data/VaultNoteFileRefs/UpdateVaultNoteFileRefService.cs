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
    /// Updates an existing VaultNoteFileRef link between a VaultNote and VaultFileRef.
    /// This does not update the underlying VaultNote or VaultFileRef.
    /// </summary>
    public class UpdateVaultNoteFileRefService : ApiServiceBase<UpdateVaultNoteFileRefRequest, UpdateVaultNoteFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultNoteFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultNoteFileRefResponse DoWork(UpdateVaultNoteFileRefRequest request)
        {
            var response = new UpdateVaultNoteFileRefResponse();

            try
            {
                var noteFileRef = Context.Set<VaultNoteFileRef>().FirstOrDefault(f => f.ID == request.NoteFileRefID);

                if (noteFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNoteFileRef '{request.NoteFileRefID}' not found.";
                    return response;
                }

                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? noteFileRef.NoteID : request.NoteID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? noteFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != noteFileRef.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != noteFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != noteFileRef.NoteID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != noteFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultNoteFileRef>().Any(f => f.ID != noteFileRef.ID && f.NoteID == noteID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this note.";
                        return response;
                    }
                }

                noteFileRef.NoteID = noteID;
                noteFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    noteFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    noteFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    noteFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    noteFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    noteFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    noteFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    noteFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    noteFileRef.IdentityList = request.IdentityList;

                noteFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultNoteFileRefService), $"Updated VaultNoteFileRef [{noteFileRef.ID}] Note [{noteFileRef.NoteID}] FileRef [{noteFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteFileRef", noteFileRef.ID, "Updated");

                response.NoteFileRefID = noteFileRef.ID;
                response.NoteFileRef = noteFileRef;
                response.UserMessage = "Vault note file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultNoteFileRefService), "Error updating VaultNoteFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault note file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultNoteFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? NoteID { get; set; }

        [MaxLength(128)]
        public string? FileRefID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteFileRefID))
                yield return new ValidationResult("NoteFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultNoteFileRefResponse : CfkApiResponse
    {
        public string? NoteFileRefID { get; set; }
        public VaultNoteFileRef? NoteFileRef { get; set; }
    }

    #endregion
}