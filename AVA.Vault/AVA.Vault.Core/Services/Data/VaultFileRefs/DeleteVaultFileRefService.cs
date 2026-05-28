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
    /// Deletes a VaultFileRef by ID.
    /// </summary>
    public class DeleteVaultFileRefService : ApiServiceBase<DeleteVaultFileRefRequest, DeleteVaultFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultFileRefResponse DoWork(DeleteVaultFileRefRequest request)
        {
            var response = new DeleteVaultFileRefResponse();

            try
            {
                var fileRef = Context.Set<VaultFileRef>()
                    .FirstOrDefault(f => f.ID == request.FileRefId && f.VaultID == request.VaultId);

                if (fileRef == null)
                {
                    response.UserMessage = $"File reference '{request.FileRefId}' not found.";
                    response.Deleted = false;
                    return response;
                }

                Context.Set<VaultFileRef>().Remove(fileRef);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Deleted");
                Context.Flush();

                _logger.Log(nameof(DeleteVaultFileRefService),
                    $"Deleted VaultFileRef [{fileRef.ID}] '{fileRef.Name}'");

                response.Deleted     = true;
                response.UserMessage = "File reference deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultFileRefService), "Error deleting VaultFileRef.", ex);
                response.UserMessage = "An error occurred while deleting the file reference.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        [MaxLength(128)]
        public string FileRefId { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultId { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileRefId))
                yield return new ValidationResult("FileRefId is required.");
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
        }
    }

    public class DeleteVaultFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
