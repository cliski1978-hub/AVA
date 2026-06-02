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
    /// Updates an existing VaultHeaderNote link between a VaultHeader and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultHeaderNoteService : ApiServiceBase<UpdateVaultHeaderNoteRequest, UpdateVaultHeaderNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultHeaderNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultHeaderNoteResponse DoWork(UpdateVaultHeaderNoteRequest request)
        {
            var response = new UpdateVaultHeaderNoteResponse();

            try
            {
                var headerNote = Context.Set<VaultHeaderNote>().FirstOrDefault(n => n.ID == request.HeaderNoteID);

                if (headerNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeaderNote '{request.HeaderNoteID}' not found.";
                    return response;
                }

                var vaultID = string.IsNullOrWhiteSpace(request.VaultID) ? headerNote.VaultID : request.VaultID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? headerNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != headerNote.VaultID)
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != headerNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != headerNote.VaultID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != headerNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultHeaderNote>().Any(n => n.ID != headerNote.ID && n.VaultID == vaultID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this vault.";
                        return response;
                    }
                }

                headerNote.VaultID = vaultID;
                headerNote.NoteID = noteID;

                if (request.Instructions != null)
                    headerNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    headerNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    headerNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    headerNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    headerNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    headerNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    headerNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    headerNote.IdentityList = request.IdentityList;

                headerNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultHeaderNoteService), $"Updated VaultHeaderNote [{headerNote.ID}] Vault [{headerNote.VaultID}] Note [{headerNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderNote", headerNote.ID, "Updated");

                response.HeaderNoteID = headerNote.ID;
                response.HeaderNote = headerNote;
                response.UserMessage = "Vault header note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultHeaderNoteService), "Error updating VaultHeaderNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault header note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultHeaderNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string HeaderNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

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
            if (string.IsNullOrWhiteSpace(HeaderNoteID))
                yield return new ValidationResult("HeaderNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultHeaderNoteResponse : CfkApiResponse
    {
        public string? HeaderNoteID { get; set; }
        public VaultHeaderNote? HeaderNote { get; set; }
    }

    #endregion
}