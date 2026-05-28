using System;
using System.Collections.Generic;
using System.Linq;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Utils
{
    /// <summary>
    /// Provides helper methods to query Vault notes and related data sets.
    /// Supports tag, keyword, and date-based filtering using VaultQueryOptions.
    /// </summary>
    public static class VaultQueryHelper
    {
        /// <summary>
        /// Filters a collection of MarkdownNotes using the given query options.
        /// </summary>
        public static List<MarkdownNote> ApplyFilters(
            IEnumerable<MarkdownNote> notes,
            VaultQueryOptions options)
        {
            if (notes == null)
                throw new ArgumentNullException(nameof(notes));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            IEnumerable<MarkdownNote> result = notes;

            // -------------------------------------------------------------
            // Tag filters
            // -------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(options.Tag))
            {
                result = result.Where(n =>
                    n.Tags.Any(t =>
                        string.Equals(t, options.Tag, StringComparison.OrdinalIgnoreCase)));
            }

            if (options.RequiredTags != null && options.RequiredTags.Any())
            {
                result = result.Where(n =>
                    n.Tags != null &&
                    options.RequiredTags.All(rt =>
                        n.Tags.Contains(rt, StringComparer.OrdinalIgnoreCase)));
            }

            // -------------------------------------------------------------
            // Keyword / content search
            // -------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(options.Keyword))
            {
                var keyword = options.Keyword.ToLowerInvariant();
                result = result.Where(n =>
                    (n.Title?.ToLowerInvariant().Contains(keyword) ?? false) ||
                    (n.Content?.ToLowerInvariant().Contains(keyword) ?? false));
            }

            // -------------------------------------------------------------
            // Date filters
            // -------------------------------------------------------------
            if (options.After.HasValue)
            {
                result = result.Where(n => n.Created >= options.After.Value);
            }

            if (options.Before.HasValue)
            {
                result = result.Where(n => n.Created <= options.Before.Value);
            }

            // -------------------------------------------------------------
            // Source / origin filtering
            // -------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(options.Source))
            {
                var src = options.Source.ToLowerInvariant();
                result = result.Where(n =>
                    n.Tags.Any(t => t.ToLowerInvariant() == src) ||
                    (n.Content?.ToLowerInvariant().Contains(src) ?? false));
            }

            // -------------------------------------------------------------
            // Sorting
            // -------------------------------------------------------------
            result = options.SortDescending
                ? result.OrderByDescending(n => n.Created)
                : result.OrderBy(n => n.Created);

            return result.ToList();
        }

        /// <summary>
        /// Finds all unique tags across the provided notes.
        /// </summary>
        public static List<string> GetAllTags(IEnumerable<MarkdownNote> notes)
        {
            if (notes == null)
                throw new ArgumentNullException(nameof(notes));

            return notes
                .Where(n => n.Tags != null)
                .SelectMany(n => n.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>
        /// Performs a simple full-text search across note titles and content.
        /// </summary>
        public static List<MarkdownNote> Search(IEnumerable<MarkdownNote> notes, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<MarkdownNote>();

            var keyword = term.ToLowerInvariant();
            return notes
                .Where(n => (n.Title?.ToLowerInvariant().Contains(keyword) ?? false) ||
                            (n.Content?.ToLowerInvariant().Contains(keyword) ?? false))
                .ToList();
        }
    }
}
