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
    /// Creates and persists a new VaultProjectNote link between a VaultProject and VaultNote.
    /// </summary>
    public class CreateVaultProjectNoteService : ApiServiceBase<CreateVaultProjectNoteRequest, CreateVaultProjectNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultProjectNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultProjectNoteResponse DoWork(CreateVaultProjectNoteRequest request)
        {
            var response = new CreateVaultProjectNoteResponse();

            try
            {
                var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                if (!projectExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProject [{request.ProjectID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultProjectNote>().Any(n => n.ID == request.ProjectNoteID || (n.ProjectID == request.ProjectID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this project.";
                    return response;
                }

                var projectNote = new VaultProjectNote
                {
                    ID = string.IsNullOrWhiteSpace(request.ProjectNoteID) ? Guid.NewGuid().ToString() : request.ProjectNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    ProjectID = request.ProjectID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultProjectNote>().Add(projectNote);
                Context.Flush();

                response.ProjectNoteID = projectNote.ID;
                response.ProjectNote = projectNote;
                response.UserMessage = "Vault project note link created successfully.";

                _logger.Log(nameof(CreateVaultProjectNoteService), $"Created VaultProjectNote [{projectNote.ID}] Project [{projectNote.ProjectID}] Note [{projectNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectNote", projectNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultProjectNoteService), "Error creating VaultProjectNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault project note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultProjectNoteRequest : CfkAuthorizedApiRequest
    {
        public string? ProjectNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string ProjectID { get; set; }

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
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");

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

    public class CreateVaultProjectNoteResponse : CfkApiResponse
    {
        public string? ProjectNoteID { get; set; }
        public VaultProjectNote? ProjectNote { get; set; }
    }

    #endregion
}