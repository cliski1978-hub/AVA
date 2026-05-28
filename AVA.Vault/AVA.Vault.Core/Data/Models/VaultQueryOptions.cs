using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    /// <summary>
    /// Represents runtime query options and filters used when retrieving VaultNotes.
    /// Not persisted in the database; purely functional for data services.
    /// </summary>
    [NotMapped]
    public class VaultQueryOptions
    {
        /// <summary>
        /// Target vault scope for the query.
        /// </summary>
        public string VaultID { get; set; }

        /// <summary>
        /// Optional project scope.
        /// </summary>
        public string ProjectID { get; set; }

        /// <summary>
        /// Search text or keyword filter.
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// Restrict results to notes with a specific tag.
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// Optional date range.
        /// </summary>
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Optional ordering field and direction.
        /// </summary>
        public string OrderBy { get; set; } = "CreatedAt";
        public bool Descending { get; set; } = true;

        /// <summary>
        /// Pagination parameters.
        /// </summary>
        public int Limit { get; set; } = 50;
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Optional list of tag names or keywords to match any.
        /// </summary>
        public List<string> IncludeTags { get; set; }

        /// <summary>
        /// Optional list of tag names or keywords to exclude.
        /// </summary>
        public List<string> ExcludeTags { get; set; }

        public VaultQueryOptions()
        {
            IncludeTags = new List<string>();
            ExcludeTags = new List<string>();
        }
    }
}
