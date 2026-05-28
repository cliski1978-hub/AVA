using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Retrieves a single VaultNote by ID, including Tags, Links, and Metadata.
    /// </summary>
    public class GetVaultNoteService : ApiServiceBase<GetVaultNoteRequest, GetVaultNoteResponse>
    {
        private readonly VaultLogger _logger;

        public GetVaultNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override GetVaultNoteResponse DoWork(GetVaultNoteRequest request)
        {
            var response = new GetVaultNoteResponse();

            try
            {
                var note = Context.Set<VaultNote>()
                    .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                    .Include(n => n.OutgoingRelations)
                    .Include(n => n.IncomingRelations)
                    .Include(n => n.Metadata)
                    .FirstOrDefault(n => n.ID == request.NoteID && n.VaultID == request.VaultID);

                if (note == null)
                {
                    response.UserMessage = $"Note '{request.NoteID}' not found in vault '{request.VaultID}'.";
                    return response;
                }

                response.Note        = note;
                response.UserMessage = "Note retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetVaultNoteService), "Error retrieving VaultNote.", ex);
                response.UserMessage = "An error occurred while retrieving the note.";
            }

            return response;
        }
    }

    #region Models

    public class GetVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");
        }
    }

    public class GetVaultNoteResponse : CfkApiResponse
    {
        public VaultNote? Note { get; set; }
    }

    #endregion
}
