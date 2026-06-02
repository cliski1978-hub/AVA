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
    /// Updates an existing VaultMetadata record.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultMetadataService : ApiServiceBase<UpdateVaultMetadataRequest, UpdateVaultMetadataResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultMetadataService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultMetadataResponse DoWork(UpdateVaultMetadataRequest request)
        {
            var response = new UpdateVaultMetadataResponse();

            try
            {
                var metadata = Context.Set<VaultMetadata>().FirstOrDefault(m => m.ID == request.MetadataID);

                if (metadata == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultMetadata '{request.MetadataID}' not found.";
                    return response;
                }

                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? metadata.NoteID : request.NoteID;
                var key = string.IsNullOrWhiteSpace(request.Key) ? metadata.Key : request.Key;

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != metadata.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != metadata.NoteID) || (!string.IsNullOrWhiteSpace(request.Key) && request.Key != metadata.Key))
                {
                    var duplicateExists = Context.Set<VaultMetadata>().Any(m => m.ID != metadata.ID && m.NoteID == noteID && m.Key.ToLower() == key.ToLower());

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"Metadata key '{key}' already exists for this note.";
                        return response;
                    }
                }

                metadata.NoteID = noteID;
                metadata.Key = key;

                if (request.Value != null)
                    metadata.Value = request.Value;

                if (request.PrimaryIdentityId != null)
                    metadata.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    metadata.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    metadata.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    metadata.IdentityList = request.IdentityList;

                metadata.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                response.MetadataID = metadata.ID;
                response.Metadata = metadata;
                response.UserMessage = "Vault metadata updated successfully.";

                _logger.Log(nameof(UpdateVaultMetadataService), $"Updated VaultMetadata [{metadata.ID}] Key [{metadata.Key}] Note [{metadata.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", metadata.ID, "Updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultMetadataService), "Error updating VaultMetadata.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault metadata.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string MetadataID { get; set; }

        [MaxLength(128)]
        public string? Key { get; set; }

        public string? Value { get; set; }

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
            if (string.IsNullOrWhiteSpace(MetadataID))
                yield return new ValidationResult("MetadataID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultMetadataResponse : CfkApiResponse
    {
        public string? MetadataID { get; set; }
        public VaultMetadata? Metadata { get; set; }
    }

    #endregion
}