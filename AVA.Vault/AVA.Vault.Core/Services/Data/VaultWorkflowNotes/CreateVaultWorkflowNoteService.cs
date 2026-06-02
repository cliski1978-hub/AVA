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
    /// Creates and persists a new VaultWorkflowNote link between a VaultWorkflow and VaultNote.
    /// </summary>
    public class CreateVaultWorkflowNoteService : ApiServiceBase<CreateVaultWorkflowNoteRequest, CreateVaultWorkflowNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowNoteResponse DoWork(CreateVaultWorkflowNoteRequest request)
        {
            var response = new CreateVaultWorkflowNoteResponse();

            try
            {
                var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                if (!workflowExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflow [{request.WorkflowID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowNote>().Any(n => n.ID == request.WorkflowNoteID || (n.WorkflowID == request.WorkflowID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this workflow.";
                    return response;
                }

                var workflowNote = new VaultWorkflowNote
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowNoteID) ? Guid.NewGuid().ToString() : request.WorkflowNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowID = request.WorkflowID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowNote>().Add(workflowNote);
                Context.Flush();

                // Set response before logging - if logging fails the created entity is still returned
                response.WorkflowNoteID = workflowNote.ID;
                response.WorkflowNote = workflowNote;
                response.UserMessage = "Vault workflow note link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowNoteService), $"Created VaultWorkflowNote [{workflowNote.ID}] Workflow [{workflowNote.WorkflowID}] Note [{workflowNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNote", workflowNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowNoteService), "Error creating VaultWorkflowNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow note link.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultWorkflowNoteRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowID))
                yield return new ValidationResult("WorkflowID is required.");

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

    public class CreateVaultWorkflowNoteResponse : CfkApiResponse
    {
        public string? WorkflowNoteID { get; set; }
        public VaultWorkflowNote? WorkflowNote { get; set; }
    }

    #endregion
}