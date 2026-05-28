using System;
using System.Collections.Generic;
using System.Linq;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Logger;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Handles project-level organization, filtering, and tagging of Vault notes.
    /// </summary>
    public class VaultProjectManager
    {
        private readonly VaultService _vault;
        private readonly VaultLogger _logger;

        public VaultProjectManager(VaultService vault, VaultLogger logger)
        {
            _vault = vault ?? throw new ArgumentNullException(nameof(vault));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void AddProjectTag(string projectTag, MarkdownNote note)
        {
            if (string.IsNullOrWhiteSpace(projectTag))
                throw new ArgumentException("Project tag cannot be null or empty.", nameof(projectTag));

            if (note.Tags == null)
                note.Tags = new List<string>();

            if (!note.Tags.Contains(projectTag, StringComparer.OrdinalIgnoreCase))
            {
                note.Tags.Add(projectTag);
                _logger.Log("VaultProjectManager", $"Added tag '{projectTag}' to note '{note.Id}'.");
            }
        }

        public List<MarkdownNote> FilterNotes(VaultQueryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            IEnumerable<MarkdownNote> result = _vault.Notes;

            if (!string.IsNullOrWhiteSpace(options.Tag))
            {
                result = result.Where(n => n.Tags != null &&
                                           n.Tags.Any(t => t.Equals(options.Tag, StringComparison.OrdinalIgnoreCase)));
            }

            if (options.RequiredTags != null && options.RequiredTags.Any())
            {
                result = result.Where(n => n.Tags != null &&
                                           options.RequiredTags.All(rt =>
                                               n.Tags.Contains(rt, StringComparer.OrdinalIgnoreCase)));
            }

            if (options.After.HasValue)
                result = result.Where(n => n.Created >= options.After.Value);

            if (options.Before.HasValue)
                result = result.Where(n => n.Created <= options.Before.Value);

            if (!string.IsNullOrWhiteSpace(options.Source))
            {
                result = result.Where(n => n.Tags != null &&
                                           n.Tags.Any(t => t.Equals(options.Source, StringComparison.OrdinalIgnoreCase)));
            }

            var filtered = result.ToList();

            _logger.Log("VaultProjectManager", $"Filtered {filtered.Count} notes using VaultQueryOptions.");
            return filtered;
        }

        public List<MarkdownNote> GetNotesForProject(string projectTag)
        {
            if (string.IsNullOrWhiteSpace(projectTag))
                throw new ArgumentException("Project tag cannot be null or empty.", nameof(projectTag));

            var result = _vault.Notes
                .Where(n => n.Tags != null && n.Tags.Contains(projectTag, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _logger.Log("VaultProjectManager", $"Fetched {result.Count} notes with tag '{projectTag}'.");
            return result;
        }

        public List<string> GetAllProjectTags()
        {
            var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var note in _vault.Notes)
            {
                if (note.Tags == null) continue;
                foreach (var tag in note.Tags)
                    tagSet.Add(tag);
            }

            _logger.Log("VaultProjectManager", $"Extracted {tagSet.Count} unique tags from vault.");
            return tagSet.ToList();
        }

        public bool NoteBelongsToProject(MarkdownNote note, string projectTag)
        {
            return note.Tags != null &&
                   note.Tags.Any(t => t.Equals(projectTag, StringComparison.OrdinalIgnoreCase));
        }
    }
}
