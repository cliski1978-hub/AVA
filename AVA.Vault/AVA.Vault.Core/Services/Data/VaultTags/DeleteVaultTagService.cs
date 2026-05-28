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
    /// Deletes a VaultTag and detaches it from any associated VaultNotes.
    /// </summary>
    public class DeleteVaultTagService : ApiServiceBase<DeleteVaultTagRequest, DeleteVaultTagResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultTagService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultTagResponse DoWork(DeleteVaultTagRequest request)
        {
            var response = new DeleteVaultTagResponse();

            try
            {
                var tag = Context.Set<VaultTag>()
                    .FirstOrDefault(t => t.ID == request.TagID && t.ProjectID == request.VaultID);

                if (tag == null)
                {
                    response.UserMessage = "Vault tag not found.";
                    response.Deleted = false;
                    return response;
                }

                // Detach tag from notes
                foreach (var jt in Context.Set<VaultNoteVaultTag>().Where(jt => jt.TagID == tag.ID).ToList())
                    Context.Set<VaultNoteVaultTag>().Remove(jt);

                Context.Set<VaultTag>().Remove(tag);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultTag", tag.ID, "Deleted");
                Context.Flush();

                _logger.Log(nameof(DeleteVaultTagService),
                    $"Deleted VaultTag [{tag.ID}] '{tag.Name}' from Vault={tag.ProjectID}");

                response.Deleted = true;
                response.UserMessage = "Vault tag deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultTagService), "Error deleting VaultTag.", ex);
                response.UserMessage = "An error occurred while deleting the VaultTag.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultTagRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string TagID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(TagID))
                yield return new ValidationResult("TagID is required.");
        }
    }

    public class DeleteVaultTagResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
