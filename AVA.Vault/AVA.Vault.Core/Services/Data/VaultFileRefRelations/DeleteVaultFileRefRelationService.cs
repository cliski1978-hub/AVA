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
    /// Deletes a VaultFileRefRelation between two VaultFileRefs.
    /// This does not delete either underlying VaultFileRef.
    /// </summary>
    public class DeleteVaultFileRefRelationService : ApiServiceBase<DeleteVaultFileRefRelationRequest, DeleteVaultFileRefRelationResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultFileRefRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultFileRefRelationResponse DoWork(DeleteVaultFileRefRelationRequest request)
        {
            var response = new DeleteVaultFileRefRelationResponse();

            try
            {
                var fileRefRelation = Context.Set<VaultFileRefRelation>().FirstOrDefault(r => r.ID == request.FileRefRelationID);

                if (fileRefRelation == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault file reference relation not found.";
                    response.Deleted = false;
                    return response;
                }

                var sourceFileRefId = fileRefRelation.SourceFileRefID;
                var targetFileRefId = fileRefRelation.TargetFileRefID;

                Context.Set<VaultFileRefRelation>().Remove(fileRefRelation);
                Context.Flush();

                response.Deleted = true;
                response.FileRefRelationID = request.FileRefRelationID;
                response.SourceFileRefID = sourceFileRefId;
                response.TargetFileRefID = targetFileRefId;
                response.UserMessage = "Vault file reference relation deleted successfully.";

                _logger.Log(nameof(DeleteVaultFileRefRelationService), $"Deleted VaultFileRefRelation [{request.FileRefRelationID}] SourceFileRef [{sourceFileRefId}] TargetFileRef [{targetFileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefRelation", request.FileRefRelationID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here if needed.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultFileRefRelationService), "Error deleting VaultFileRefRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault file reference relation.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultFileRefRelationRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefRelationID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileRefRelationID))
                yield return new ValidationResult("FileRefRelationID is required.");
        }
    }

    public class DeleteVaultFileRefRelationResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? FileRefRelationID { get; set; }
        public string? SourceFileRefID { get; set; }
        public string? TargetFileRefID { get; set; }
    }

    #endregion
}