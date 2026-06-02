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
    /// Updates an existing VaultFileRefNote link between a VaultFileRef and VaultNote.
    /// This does not update the underlying VaultNote or VaultFileRef.
    /// </summary>
    public class UpdateVaultFileRefNoteService : ApiServiceBase<UpdateVaultFileRefNoteRequest, UpdateVaultFileRefNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultFileRefNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultFileRefNoteResponse DoWork(UpdateVaultFileRefNoteRequest request)
        {
            var response = new UpdateVaultFileRefNoteResponse();

            try
            {
                var fileRefNote = Context.Set<VaultFileRefNote>().FirstOrDefault(n => n.ID == request.FileRefNoteID);

                if (fileRefNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRefNote '{request.FileRefNoteID}' not found.";
                    return response;
                }

                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? fileRefNote.FileRefID : request.FileRefID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? fileRefNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != fileRefNote.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != fileRefNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != fileRefNote.FileRefID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != fileRefNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultFileRefNote>().Any(n => n.ID != fileRefNote.ID && n.FileRefID == fileRefID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this file reference.";
                        return response;
                    }
                }

                fileRefNote.FileRefID = fileRefID;
                fileRefNote.NoteID = noteID;

                if (request.Instructions != null)
                    fileRefNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    fileRefNote.IsRequired = request.IsRequired.Value;

                if (request.NoteOrder.HasValue)
                    fileRefNote.NoteOrder = request.NoteOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    fileRefNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    fileRefNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    fileRefNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    fileRefNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    fileRefNote.IdentityList = request.IdentityList;

                fileRefNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultFileRefNoteService), $"Updated VaultFileRefNote [{fileRefNote.ID}] FileRef [{fileRefNote.FileRefID}] Note [{fileRefNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefNote", fileRefNote.ID, "Updated");

                response.FileRefNoteID = fileRefNote.ID;
                response.FileRefNote = fileRefNote;
                response.UserMessage = "Vault file reference note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultFileRefNoteService), "Error updating VaultFileRefNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault file reference note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultFileRefNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? NoteOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? FileRefID { get; set; }

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
            if (string.IsNullOrWhiteSpace(FileRefNoteID))
                yield return new ValidationResult("FileRefNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultFileRefNoteResponse : CfkApiResponse
    {
        public string? FileRefNoteID { get; set; }
        public VaultFileRefNote? FileRefNote { get; set; }
    }

    #endregion
}