using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Creates and persists a new VaultFileRef attached to a Vault, Project, or Session.
    /// </summary>
    public class CreateVaultFileRefService : ApiServiceBase<CreateVaultFileRefRequest, CreateVaultFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultFileRefResponse DoWork(CreateVaultFileRefRequest request)
        {
            var response = new CreateVaultFileRefResponse();

            try
            {
                var fileRef = new VaultFileRef
                {
                    ID        = Guid.NewGuid().ToString(),
                    VaultID   = request.VaultId,
                    ProjectID = request.ProjectId,
                    SessionID = request.SessionId,
                    Path      = request.Path,
                    Name      = request.Name,
                    CreatedAt = DateTime.UtcNow
                };

                Context.Set<VaultFileRef>().Add(fileRef);
                Context.Flush();

                _logger.Log(nameof(CreateVaultFileRefService),
                    $"Created VaultFileRef [{fileRef.ID}] '{fileRef.Name}' in Vault {fileRef.VaultID}");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Created");

                response.FileRefId   = fileRef.ID;
                response.FileRef     = fileRef;
                response.UserMessage = "File reference created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultFileRefService), "Error creating VaultFileRef.", ex);
                response.UserMessage = "An error occurred while creating the file reference.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        [MaxLength(128)]
        public string VaultId { get; set; }

        [MaxLength(128)]
        public string? ProjectId { get; set; }

        [MaxLength(128)]
        public string? SessionId { get; set; }

        [Required]
        public string Path { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
            if (string.IsNullOrWhiteSpace(Path))
                yield return new ValidationResult("Path is required.");
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");
        }
    }

    public class CreateVaultFileRefResponse : CfkApiResponse
    {
        public string? FileRefId { get; set; }
        public VaultFileRef? FileRef { get; set; }
    }

    #endregion
}
