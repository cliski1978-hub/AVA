using System;
using System.Collections.Generic;
using System.Linq;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Core service representing a single active Vault context.
    /// Manages notes, metadata, and in-memory graph relationships.
    /// </summary>
    public class VaultService : IVault
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Configuration that defines how this vault is persisted and managed.
        /// </summary>
        public VaultInstanceConfig Config { get; set; } = new VaultInstanceConfig();

        /// <summary>
        /// Collection of notes stored in this vault.
        /// </summary>
        public List<MarkdownNote> Notes { get; set; } = new();

        /// <summary>
        /// Graph representation of relationships between notes.
        /// </summary>
        public NoteGraph Graph { get; set; } = new();

        public VaultService(string name)
        {
            Name = name;
            Config.DisplayName = name;
        }

        // -------------------------------------------------------------
        // CRUD operations for notes
        // -------------------------------------------------------------

        public void AddNote(MarkdownNote note)
        {
            Notes.Add(note);
            LastModified = DateTime.UtcNow;
            Graph.BuildGraph(Notes);
        }

        public void RemoveNote(string noteId)
        {
            var toRemove = Notes.FirstOrDefault(n => n.Id == noteId);
            if (toRemove != null)
            {
                Notes.Remove(toRemove);
                LastModified = DateTime.UtcNow;
                Graph.BuildGraph(Notes);
            }
        }

        public MarkdownNote? FindByTitle(string title)
        {
            return Notes.FirstOrDefault(n =>
                string.Equals(n.Title, title, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<MarkdownNote> FindByTag(string tag)
        {
            return Notes.Where(n =>
                n.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        }
    }
}
