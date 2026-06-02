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
    /// Creates and persists a new VaultMetadata record for a VaultNote.
    /// </summary>
    public class CreateVaultMetadataService : ApiServiceBase<CreateVaultMetadataRequest, CreateVaultMetadataResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultMetadataService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultMetadataResponse DoWork(CreateVaultMetadataRequest request)
        {
            var response = new CreateVaultMetadataResponse();

            try
            {
                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultMetadata>().Any(m => m.ID == request.MetadataID || (m.NoteID == request.NoteID && m.Key.ToLower() == request.Key.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"Metadata key '{request.Key}' already exists for this note.";
                    return response;
                }

                var metadata = new VaultMetadata
                {
                    ID = string.IsNullOrWhiteSpace(request.MetadataID) ? Guid.NewGuid().ToString() : request.MetadataID,
                    Key = request.Key,
                    Value = request.Value,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultMetadata>().Add(metadata);
                Context.Flush();

                response.MetadataID = metadata.ID;
                response.Metadata = metadata;
                response.UserMessage = "Vault metadata created successfully.";

                _logger.Log(nameof(CreateVaultMetadataService), $"Created VaultMetadata [{metadata.ID}] Key [{metadata.Key}] Note [{metadata.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", metadata.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultMetadataService), "Error creating VaultMetadata.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault metadata.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        public string? MetadataID { get; set; }

        [Required]
        [MaxLength(128)]
        public string Key { get; set; }

        public string? Value { get; set; }

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
            if (string.IsNullOrWhiteSpace(Key))
                yield return new ValidationResult("Key is required.");

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

    public class CreateVaultMetadataResponse : CfkApiResponse
    {
        public string? MetadataID { get; set; }
        public VaultMetadata? Metadata { get; set; }
    }

    #endregion
}