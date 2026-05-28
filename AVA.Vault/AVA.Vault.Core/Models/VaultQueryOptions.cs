using System;
using System.Collections.Generic;

namespace AVA.Vault.Core.Models
{
    /// <summary>
    /// Represents filtering and search options for querying vault notes.
    /// Used by VaultQueryHelper and higher-level search services.
    /// </summary>
    public class VaultQueryOptions
    {
        /// <summary>
        /// Filter for notes containing this specific tag.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Optional list of tags that all must be present on a note.
        /// </summary>
        public List<string>? RequiredTags { get; set; }

        /// <summary>
        /// Keyword to search within note title or content.
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Only return notes created on or after this date.
        /// </summary>
        public DateTime? After { get; set; }

        /// <summary>
        /// Only return notes created on or before this date.
        /// </summary>
        public DateTime? Before { get; set; }

        /// <summary>
        /// Optional source or origin filter (e.g., agent name, import origin).
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Whether to sort results in descending order (newest first).
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }
}
