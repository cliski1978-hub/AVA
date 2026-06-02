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
    /// Creates and persists a new VaultNote.
    /// VaultID is the required ownership/root container.
    /// SessionID is optional and should only be used when a note is directly tied to a session.
    /// </summary>
    public class CreateVaultNoteService : ApiServiceBase<CreateVaultNoteRequest, CreateVaultNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultNoteResponse DoWork(CreateVaultNoteRequest request)
        {
            var response = new CreateVaultNoteResponse();

            try
            {
                var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                if (!vaultExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.SessionID))
                {
                    var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                    if (!sessionExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultSession [{request.SessionID}] was not found.";
                        return response;
                    }
                }

                var exists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID || (n.VaultID == request.VaultID && n.Title.ToLower() == request.Title.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A note titled '{request.Title}' already exists in this vault.";
                    return response;
                }

                var note = new VaultNote
                {
                    ID = string.IsNullOrWhiteSpace(request.NoteID) ? Guid.NewGuid().ToString() : request.NoteID,
                    Content = request.Content,
                    EmbeddingJson = request.EmbeddingJson,
                    IsPinned = request.IsPinned,
                    IsSynced = request.IsSynced,
                    IsTemplate = request.IsTemplate,
                    MetadataJson = request.MetadataJson,
                    SortOrder = request.SortOrder,
                    TemplateName = request.TemplateName,
                    Summary = request.Summary,
                    Title = request.Title,
                    VaultID = request.VaultID,
                    SessionID = request.SessionID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultNote>().Add(note);
                Context.Flush();

                if (!string.IsNullOrWhiteSpace(request.ProjectID))
                {
                    var projectNote = new VaultProjectNote
                    {
                        ID        = Guid.NewGuid().ToString(),
                        NoteID    = note.ID,
                        ProjectID = request.ProjectID,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    Context.Set<VaultProjectNote>().Add(projectNote);
                    Context.Flush();
                }

                response.NoteID = note.ID;
                response.Note = note;
                response.UserMessage = "Vault note created successfully.";

                _logger.Log(nameof(CreateVaultNoteService), $"Created VaultNote [{note.ID}] '{note.Title}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultNoteService), "Error creating VaultNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault note.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultNoteRequest : CfkAuthorizedApiRequest
    {
        public string? NoteID { get; set; }

        public string? Content { get; set; }

        public string? EmbeddingJson { get; set; }

        public bool IsPinned { get; set; }

        public bool IsSynced { get; set; }

        public bool IsTemplate { get; set; }

        public string? MetadataJson { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(256)]
        public string? TemplateName { get; set; }

        [MaxLength(512)]
        public string? Summary { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Title))
                yield return new ValidationResult("Title is required.");

            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultNoteResponse : CfkApiResponse
    {
        public string? NoteID { get; set; }
        public VaultNote? Note { get; set; }
    }

    #endregion
}