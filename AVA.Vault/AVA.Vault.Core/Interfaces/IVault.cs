using System;
using System.Collections.Generic;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Graph;

namespace AVA.Vault.Core.Interfaces
{
    public interface IVault
    {
        string Id { get; }
        string Name { get; }
        DateTime Created { get; }
        DateTime LastModified { get; }

        List<MarkdownNote> Notes { get; }
        NoteGraph Graph { get; }

        void AddNote(MarkdownNote note);
        void RemoveNote(string noteId);

        MarkdownNote FindByTitle(string title);
        IEnumerable<MarkdownNote> FindByTag(string tag);
    }
}
