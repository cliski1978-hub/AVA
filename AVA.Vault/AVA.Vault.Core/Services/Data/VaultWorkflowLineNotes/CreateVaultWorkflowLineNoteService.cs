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
    /// Creates and persists a new VaultWorkflowLineNote link between a VaultWorkflowLine and VaultNote.
    /// </summary>
    public class CreateVaultWorkflowLineNoteService : ApiServiceBase<CreateVaultWorkflowLineNoteRequest, CreateVaultWorkflowLineNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowLineNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowLineNoteResponse DoWork(CreateVaultWorkflowLineNoteRequest request)
        {
            var response = new CreateVaultWorkflowLineNoteResponse();

            try
            {
                var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                if (!workflowLineExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLine [{request.WorkflowLineID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowLineNote>().Any(n => n.ID == request.WorkflowLineNoteID || (n.WorkflowLineID == request.WorkflowLineID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this workflow line.";
                    return response;
                }

                var workflowLineNote = new VaultWorkflowLineNote
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowLineNoteID) ? Guid.NewGuid().ToString() : request.WorkflowLineNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowLineID = request.WorkflowLineID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowLineNote>().Add(workflowLineNote);
                Context.Flush();

                response.WorkflowLineNoteID = workflowLineNote.ID;
                response.WorkflowLineNote = workflowLineNote;
                response.UserMessage = "Vault workflow line note link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowLineNoteService), $"Created VaultWorkflowLineNote [{workflowLineNote.ID}] WorkflowLine [{workflowLineNote.WorkflowLineID}] Note [{workflowLineNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineNote", workflowLineNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowLineNoteService), "Error creating VaultWorkflowLineNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow line note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowLineNoteRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowLineNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowLineID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineID))
                yield return new ValidationResult("WorkflowLineID is required.");

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

    public class CreateVaultWorkflowLineNoteResponse : CfkApiResponse
    {
        public string? WorkflowLineNoteID { get; set; }
        public VaultWorkflowLineNote? WorkflowLineNote { get; set; }
    }

    #endregion
}