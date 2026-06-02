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
    /// Updates an existing VaultProjectNote link between a VaultProject and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultProjectNoteService : ApiServiceBase<UpdateVaultProjectNoteRequest, UpdateVaultProjectNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultProjectNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultProjectNoteResponse DoWork(UpdateVaultProjectNoteRequest request)
        {
            var response = new UpdateVaultProjectNoteResponse();

            try
            {
                var projectNote = Context.Set<VaultProjectNote>().FirstOrDefault(n => n.ID == request.ProjectNoteID);

                if (projectNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProjectNote '{request.ProjectNoteID}' not found.";
                    return response;
                }

                var projectID = string.IsNullOrWhiteSpace(request.ProjectID) ? projectNote.ProjectID : request.ProjectID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? projectNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.ProjectID) && request.ProjectID != projectNote.ProjectID)
                {
                    var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                    if (!projectExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultProject '{request.ProjectID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != projectNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.ProjectID) && request.ProjectID != projectNote.ProjectID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != projectNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultProjectNote>().Any(n => n.ID != projectNote.ID && n.ProjectID == projectID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this project.";
                        return response;
                    }
                }

                projectNote.ProjectID = projectID;
                projectNote.NoteID = noteID;

                if (request.Instructions != null)
                    projectNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    projectNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    projectNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    projectNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    projectNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    projectNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    projectNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    projectNote.IdentityList = request.IdentityList;

                projectNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultProjectNoteService), $"Updated VaultProjectNote [{projectNote.ID}] Project [{projectNote.ProjectID}] Note [{projectNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectNote", projectNote.ID, "Updated");

                response.ProjectNoteID = projectNote.ID;
                response.ProjectNote = projectNote;
                response.UserMessage = "Vault project note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultProjectNoteService), "Error updating VaultProjectNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault project note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultProjectNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

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
            if (string.IsNullOrWhiteSpace(ProjectNoteID))
                yield return new ValidationResult("ProjectNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultProjectNoteResponse : CfkApiResponse
    {
        public string? ProjectNoteID { get; set; }
        public VaultProjectNote? ProjectNote { get; set; }
    }

    #endregion
}