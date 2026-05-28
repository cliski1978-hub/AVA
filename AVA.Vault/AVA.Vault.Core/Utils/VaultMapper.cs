using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Utils
{
    /// <summary>
    /// Handles mapping and normalization between Markdown / JSON
    /// note data and strongly-typed Vault model representations.
    /// This mapper exists only to standardize import/export and persistence
    /// for MarkdownNote and related model classes.
    /// </summary>
    public static class VaultMapper
    {
        // -------------------------------------------------------------
        // Markdown ? VaultNote
        // -------------------------------------------------------------

        /// <summary>
        /// Converts a Markdown (.md) file into a VaultNote model.
        /// Supports optional YAML-style front-matter for metadata.
        /// </summary>
        public static MarkdownNote FromMarkdown(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Markdown note file not found.", filePath);

            var content = File.ReadAllText(filePath);
            var note = new MarkdownNote
            {
                Id = Path.GetFileNameWithoutExtension(filePath),
                Title = Path.GetFileNameWithoutExtension(filePath),
                Content = content,
                Tags = new List<string>(),
                Created = File.GetCreationTimeUtc(filePath),
                Modified = File.GetLastWriteTimeUtc(filePath)
            };

            // future enhancement: parse YAML front-matter for tags or attributes
            return note;
        }

        /// <summary>
        /// Converts a VaultNote into a Markdown string ready for file output.
        /// </summary>
        public static string ToMarkdown(MarkdownNote note)
        {
            return $"# {note.Title}\n\n{note.Content}\n";
        }

        // -------------------------------------------------------------
        // JSON ? VaultNote
        // -------------------------------------------------------------

        public static MarkdownNote FromJson(string json)
        {
            return JsonSerializer.Deserialize<MarkdownNote>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new MarkdownNote();
        }

        public static string ToJson(MarkdownNote note, bool indented = true)
        {
            return JsonSerializer.Serialize(note, new JsonSerializerOptions
            {
                WriteIndented = indented
            });
        }

        // -------------------------------------------------------------
        // Bulk helpers
        // -------------------------------------------------------------

        public static List<MarkdownNote> FromMarkdownDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return new List<MarkdownNote>();

            return Directory.GetFiles(folderPath, "*.md")
                            .Select(FromMarkdown)
                            .ToList();
        }

        public static void ToMarkdownDirectory(IEnumerable<MarkdownNote> notes, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            foreach (var note in notes)
            {
                var filePath = Path.Combine(folderPath, $"{note.Title}.md");
                File.WriteAllText(filePath, ToMarkdown(note));
            }
        }

        // -------------------------------------------------------------
        // JSON List helpers (for backups or API exchange)
        // -------------------------------------------------------------

        public static string ToJsonList(IEnumerable<MarkdownNote> notes, bool indented = true)
        {
            return JsonSerializer.Serialize(notes, new JsonSerializerOptions
            {
                WriteIndented = indented
            });
        }

        public static List<MarkdownNote> FromJsonList(string json)
        {
            return JsonSerializer.Deserialize<List<MarkdownNote>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<MarkdownNote>();
        }
    }
}
