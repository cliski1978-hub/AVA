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
    /// Searches VaultNotes by title, content, project, session, or tag.
    /// All filters are optional and additive — unset filters are ignored.
    /// Results are ordered by UpdatedAt descending.
    /// </summary>
    public class SearchVaultNotesService : ApiServiceBase<SearchVaultNotesRequest, SearchVaultNotesResponse>
    {
        private readonly VaultLogger _logger;

        public SearchVaultNotesService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override SearchVaultNotesResponse DoWork(SearchVaultNotesRequest request)
        {
            var response = new SearchVaultNotesResponse();

            try
            {
                var query = Context.Set<VaultNote>()
                    .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                    .Include(n => n.OutgoingRelations)
                    .Include(n => n.Metadata)
                    .Where(n => n.VaultID == request.VaultID)
                    .AsQueryable();

                // Project filter
                if (!string.IsNullOrWhiteSpace(request.ProjectID))
                    query = query.Where(n => n.ProjectNotes.Any(pn => pn.ProjectID == request.ProjectID));

                // Session filter
                if (!string.IsNullOrWhiteSpace(request.SessionID))
                    query = query.Where(n => n.SessionID == request.SessionID);

                // Keyword — OR across title and content (use for general search)
                if (!string.IsNullOrWhiteSpace(request.Keyword))
                    query = query.Where(n =>
                        (n.Title != null && n.Title.Contains(request.Keyword)) ||
                        n.Content.Contains(request.Keyword));

                // Title only — additive AND filter
                if (!string.IsNullOrWhiteSpace(request.TitleContains))
                    query = query.Where(n => n.Title != null &&
                        n.Title.Contains(request.TitleContains));

                // Content only — additive AND filter
                if (!string.IsNullOrWhiteSpace(request.ContentContains))
                    query = query.Where(n => n.Content.Contains(request.ContentContains));

                // Tag filter — note must have at least one matching tag
                if (!string.IsNullOrWhiteSpace(request.Tag))
                    query = query.Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name == request.Tag));

                // Date range — created
                if (request.CreatedAfter.HasValue)
                    query = query.Where(n => n.CreatedAt >= request.CreatedAfter.Value);
                if (request.CreatedBefore.HasValue)
                    query = query.Where(n => n.CreatedAt <= request.CreatedBefore.Value);

                // Date range — updated
                if (request.UpdatedAfter.HasValue)
                    query = query.Where(n => n.UpdatedAt >= request.UpdatedAfter.Value);
                if (request.UpdatedBefore.HasValue)
                    query = query.Where(n => n.UpdatedAt <= request.UpdatedBefore.Value);

                // Sorting
                query = (request.SortBy, request.SortDescending) switch
                {
                    ("Created",      true)  => query.OrderByDescending(n => n.CreatedAt),
                    ("Created",      false) => query.OrderBy(n => n.CreatedAt),
                    ("Alphabetical", true)  => query.OrderByDescending(n => n.Title),
                    ("Alphabetical", false) => query.OrderBy(n => n.Title),
                    (_,              true)  => query.OrderByDescending(n => n.UpdatedAt),
                    (_,              false) => query.OrderBy(n => n.UpdatedAt),
                };

                var results = query
                    .Take(request.MaxResults > 0 ? request.MaxResults : 100)
                    .ToList();

                response.Notes       = results;
                response.TotalFound  = results.Count;
                response.UserMessage = $"{results.Count} note(s) found.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(SearchVaultNotesService), "Error searching VaultNotes.", ex);
                response.UserMessage = "An error occurred while searching notes.";
            }

            return response;
        }
    }

    #region Models

    public class SearchVaultNotesRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }

        // Optional filters
        public string? ProjectID { get; set; }
        public string? SessionID { get; set; }
        /// <summary>OR search across title and content. Use for general keyword search.</summary>
        public string? Keyword { get; set; }
        /// <summary>AND filter — title must contain this value.</summary>
        public string? TitleContains { get; set; }
        /// <summary>AND filter — content must contain this value.</summary>
        public string? ContentContains { get; set; }
        public string? Tag { get; set; }
        public DateTime? CreatedAfter  { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? UpdatedAfter  { get; set; }
        public DateTime? UpdatedBefore { get; set; }

        /// <summary>Sort field: "Updated" (default), "Created", "Alphabetical".</summary>
        public string SortBy { get; set; } = "Updated";

        /// <summary>True = descending (newest/Z first). Default true.</summary>
        public bool SortDescending { get; set; } = true;

        /// <summary>Max results to return. Defaults to 100.</summary>
        public int MaxResults { get; set; } = 100;

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
        }
    }

    public class SearchVaultNotesResponse : CfkApiResponse
    {
        public List<VaultNote> Notes { get; set; } = new();
        public int TotalFound { get; set; }
    }

    #endregion
}
