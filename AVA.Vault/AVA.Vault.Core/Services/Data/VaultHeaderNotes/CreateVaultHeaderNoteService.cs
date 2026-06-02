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
    /// Creates and persists a new VaultHeaderNote link between a VaultHeader and VaultNote.
    /// </summary>
    public class CreateVaultHeaderNoteService : ApiServiceBase<CreateVaultHeaderNoteRequest, CreateVaultHeaderNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultHeaderNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultHeaderNoteResponse DoWork(CreateVaultHeaderNoteRequest request)
        {
            var response = new CreateVaultHeaderNoteResponse();

            try
            {
                var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                if (!vaultExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultHeaderNote>().Any(n => n.ID == request.HeaderNoteID || (n.VaultID == request.VaultID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this vault.";
                    return response;
                }

                var headerNote = new VaultHeaderNote
                {
                    ID = string.IsNullOrWhiteSpace(request.HeaderNoteID) ? Guid.NewGuid().ToString() : request.HeaderNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    VaultID = request.VaultID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultHeaderNote>().Add(headerNote);
                Context.Flush();

                response.HeaderNoteID = headerNote.ID;
                response.HeaderNote = headerNote;
                response.UserMessage = "Vault header note link created successfully.";

                _logger.Log(nameof(CreateVaultHeaderNoteService), $"Created VaultHeaderNote [{headerNote.ID}] Vault [{headerNote.VaultID}] Note [{headerNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderNote", headerNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultHeaderNoteService), "Error creating VaultHeaderNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault header note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultHeaderNoteRequest : CfkAuthorizedApiRequest
    {
        public string? HeaderNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

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
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");

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

    public class CreateVaultHeaderNoteResponse : CfkApiResponse
    {
        public string? HeaderNoteID { get; set; }
        public VaultHeaderNote? HeaderNote { get; set; }
    }

    #endregion
}