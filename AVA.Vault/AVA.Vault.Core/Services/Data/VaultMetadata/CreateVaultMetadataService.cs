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
    /// Creates and persists a new VaultMetadata record for a given note.
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
                var existing = Context.Set<VaultMetadata>()
                    .FirstOrDefault(m =>
                        m.NoteID == request.NoteID &&
                        m.Key == request.Key);

                if (existing != null)
                {
                    response.UserMessage = "A metadata entry with this key already exists for this note.";
                    response.MetadataID = existing.ID;
                    return response;
                }

                var meta = new VaultMetadata
                {
                    ID = Guid.NewGuid().ToString(),
                    NoteID = request.NoteID,
                    Key = request.Key,
                    Value = request.Value,
                    OwnerID = request.OwnerID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Context.Set<VaultMetadata>().Add(meta);
                Context.Flush();

                _logger.Log(nameof(CreateVaultMetadataService),
                    $"Created metadata '{meta.Key}' for note {meta.NoteID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", meta.ID, "Created");

                response.MetadataID = meta.ID;
                response.UserMessage = "Metadata created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultMetadataService), "Error creating VaultMetadata.", ex);
                response.UserMessage = "An error occurred while creating the metadata entry.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID { get; set; }
        [Required] public string Key { get; set; }
        [Required] public string Value { get; set; }
        public string? OwnerID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");
            if (string.IsNullOrWhiteSpace(Key))
                yield return new ValidationResult("Key is required.");
        }
    }

    public class CreateVaultMetadataResponse : CfkApiResponse
    {
        public string? MetadataID { get; set; }
    }

    #endregion
}
