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
    /// Creates and persists a new VaultWorkflowLineStepNote link between a VaultWorkflowLineStep and VaultNote.
    /// </summary>
    public class CreateVaultWorkflowLineStepNoteService : ApiServiceBase<CreateVaultWorkflowLineStepNoteRequest, CreateVaultWorkflowLineStepNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineStepNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineStepNoteResponse DoWork(CreateVaultWorkflowLineStepNoteRequest request)
        {
            var response = new CreateVaultWorkflowLineStepNoteResponse();

            try
            {
                var workflowLineStepExists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID == request.WorkflowLineStepID);

                if (!workflowLineStepExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineStep [{request.WorkflowLineStepID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLineStepNote>().Any(n => n.ID == request.WorkflowLineStepNoteID || (n.WorkflowLineStepID == request.WorkflowLineStepID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this workflow line step.";
                    return response;
                }

                var workflowLineStepNote = new VaultWorkflowLineStepNote
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineStepNoteID) ? Guid.NewGuid().ToString() : request.WorkflowLineStepNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowLineStepID = request.WorkflowLineStepID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLineStepNote>().Add(workflowLineStepNote);
                Context.Flush();

                response.WorkflowLineStepNoteID = workflowLineStepNote.ID;
                response.WorkflowLineStepNote = workflowLineStepNote;
                response.UserMessage = "Vault workflow line step note link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineStepNoteService), $"Created VaultWorkflowLineStepNote [{workflowLineStepNote.ID}] WorkflowLineStep [{workflowLineStepNote.WorkflowLineStepID}] Note [{workflowLineStepNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepNote", workflowLineStepNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineStepNoteService), "Error creating VaultWorkflowLineStepNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line step note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowLineStepNoteRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineStepNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowLineStepID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineStepID))
                yield return new ValidationResult("WorkflowLineStepID is required.");

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

    public class CreateVaultWorkflowLineStepNoteResponse : CfkApiResponse
    {
        public string? WorkflowLineStepNoteID { get; set; }
        public VaultWorkflowLineStepNote? WorkflowLineStepNote { get; set; }
    }

    #endregion
}