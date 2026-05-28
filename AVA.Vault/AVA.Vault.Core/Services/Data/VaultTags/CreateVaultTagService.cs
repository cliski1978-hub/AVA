using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Creates and persists a new VaultTag for a given Vault.
    /// </summary>
    public class CreateVaultTagService : ApiServiceBase<CreateVaultTagRequest, CreateVaultTagResponse>
    {
        private readonly VaultLogger _logger;
        private readonly IVaultIdService _ids;

        public CreateVaultTagService(IDbContext context, VaultLogger logger, IVaultIdService ids) : base(context)
        {
            _logger = logger;
            _ids    = ids;
        }

        protected override CreateVaultTagResponse DoWork(CreateVaultTagRequest request)
        {
            var response = new CreateVaultTagResponse();

            try
            {
                var existing = Context.Set<VaultTag>()
                    .FirstOrDefault(t =>
                        t.ProjectID == request.VaultID &&
                        t.Name.ToLower() == request.Name.ToLower());

                if (existing != null)
                {
                    response.UserMessage = $"A tag named '{request.Name}' already exists in this vault.";
                    response.TagID = existing.ID;
                    return response;
                }

                var tag = new VaultTag
                {
                    ID = _ids.NewId(),
                    ProjectID = request.VaultID,
                    Name = request.Name,
                    Color = request.Color,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Context.Set<VaultTag>().Add(tag);
                Context.Flush();

                _logger.Log(nameof(CreateVaultTagService),
                    $"Created VaultTag [{tag.ID}] '{tag.Name}' in Vault={tag.ProjectID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultTag", tag.ID, "Created");

                response.TagID       = tag.ID;
                response.Tag         = tag;
                response.UserMessage = "Vault tag created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultTagService), "Error creating VaultTag.", ex);
                response.UserMessage = "An error occurred while creating the VaultTag.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultTagRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required, MaxLength(128)] public string Name { get; set; }
        [MaxLength(32)] public string? Color { get; set; }
        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Tag name is required.");
        }
    }

    public class CreateVaultTagResponse : CfkApiResponse
    {
        public string? TagID { get; set; }
        public VaultTag? Tag { get; set; }
    }

    #endregion
}
