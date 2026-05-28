using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Parsing;

namespace AVA.Vault.Core.Services
{
    public static class VaultIO
    {
        private static readonly NoteParser Parser = new();

        public static void SaveNotesToDisk(VaultService vault, string vaultPath)
        {
            var notesDir = Path.Combine(vaultPath, "notes");
            Directory.CreateDirectory(notesDir);

            foreach (var note in vault.Notes)
            {
                var fileName = $"{SanitizeFileName(note.Title)}.md";
                var fullPath = Path.Combine(notesDir, fileName);
                File.WriteAllText(fullPath, note.Content);
            }
        }

        public static void LoadNotesFromDisk(VaultService vault, string vaultPath)
        {
            var notesDir = Path.Combine(vaultPath, "notes");
            if (!Directory.Exists(notesDir)) return;

            var files = Directory.GetFiles(notesDir, "*.md");
            var notes = new List<MarkdownNote>();

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                var note = Parser.Parse(content);
                notes.Add(note);
            }

            vault.Notes = notes;
            vault.Graph.BuildGraph(notes);
        }

        private static string SanitizeFileName(string title)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                title = title.Replace(c, '_');

            return title.Replace(" ", "_").Trim();
        }
    }
}
