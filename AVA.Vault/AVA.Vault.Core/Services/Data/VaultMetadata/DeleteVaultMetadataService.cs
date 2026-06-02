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
    /// Deletes a VaultMetadata record.
    /// This does not delete the underlying VaultNote.
    /// </summary>
    public class DeleteVaultMetadataService : ApiServiceBase<DeleteVaultMetadataRequest, DeleteVaultMetadataResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultMetadataService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultMetadataResponse DoWork(DeleteVaultMetadataRequest request)
        {
            var response = new DeleteVaultMetadataResponse();

            try
            {
                var metadata = Context.Set<VaultMetadata>().FirstOrDefault(m => m.ID == request.MetadataID);

                if (metadata == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault metadata not found.";
                    response.Deleted = false;
                    return response;
                }

                var noteId = metadata.NoteID;
                var key = metadata.Key;

                Context.Set<VaultMetadata>().Remove(metadata);
                Context.Flush();

                response.Deleted = true;
                response.MetadataID = request.MetadataID;
                response.NoteID = noteId;
                response.Key = key;
                response.UserMessage = "Vault metadata deleted successfully.";

                _logger.Log(nameof(DeleteVaultMetadataService), $"Deleted VaultMetadata [{request.MetadataID}] Key [{key}] Note [{noteId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", request.MetadataID, "Deleted");

                // TODO: After note cleanup services are created, call centralized orphan evaluation here if needed.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultMetadataService), "Error deleting VaultMetadata.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault metadata.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string MetadataID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(MetadataID))
                yield return new ValidationResult("MetadataID is required.");
        }
    }

    public class DeleteVaultMetadataResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? MetadataID { get; set; }
        public string? NoteID { get; set; }
        public string? Key { get; set; }
    }

    #endregion
}