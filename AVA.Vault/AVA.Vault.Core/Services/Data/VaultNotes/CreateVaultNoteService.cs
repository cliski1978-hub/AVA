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
    /// Creates a new VaultNote within a project and vault scope.
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
                var exists = Context.Set<VaultNote>().Any(n =>
                    n.VaultID == request.VaultID &&
                    n.ProjectID == request.ProjectID &&
                    n.Title == request.Title);

                if (exists)
                {
                    response.UserMessage = "A note with this title already exists in the project.";
                    return response;
                }

                var note = new VaultNote
                {
                    ID           = Guid.NewGuid().ToString(),
                    VaultID      = request.VaultID,
                    ProjectID    = request.ProjectID,
                    SessionID    = request.SessionID,
                    Title        = request.Title ?? $"Note {DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    Content      = request.Content ?? string.Empty,
                    MetadataJson = request.MetadataJson,
                    EmbeddingJson = request.EmbeddingJson,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow,
                    IsSynced     = false
                };

                Context.Set<VaultNote>().Add(note);
                Context.Flush();

                _logger.Log(nameof(CreateVaultNoteService),
                    $"Created VaultNote [{note.ID}] '{note.Title}' in Project={note.ProjectID}, Vault={note.VaultID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Created");

                response.NoteID      = note.ID;
                response.Note        = note;
                response.UserMessage = "Vault note created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultNoteService), "Error creating VaultNote.", ex);
                response.UserMessage = "An error occurred while creating the VaultNote.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string ProjectID { get; set; }
        public string? SessionID { get; set; }
        [MaxLength(256)] public string? Title { get; set; }
        [Required] public string Content { get; set; }
        public string? MetadataJson { get; set; }
        public string? EmbeddingJson { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
            if (string.IsNullOrWhiteSpace(Content))
                yield return new ValidationResult("Content cannot be empty.");
        }
    }

    public class CreateVaultNoteResponse : CfkApiResponse
    {
        public string? NoteID { get; set; }
        public VaultNote? Note { get; set; }
    }

    #endregion
}
