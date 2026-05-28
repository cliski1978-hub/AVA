using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Utils
{
    /// <summary>
    /// Handles standardized serialization and deserialization of Vault model objects.
    /// Supports JSON and Markdown note persistence, with consistent schema control.
    /// </summary>
    public static class VaultSerializer
    {
        // -------------------------------------------------------------
        // Core JSON Options
        // -------------------------------------------------------------

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // -------------------------------------------------------------
        // Generic JSON Serialization
        // -------------------------------------------------------------

        public static string ToJson<T>(T obj, bool indented = true)
        {
            var opts = indented ? _jsonOptions : new JsonSerializerOptions(_jsonOptions) { WriteIndented = false };
            return JsonSerializer.Serialize(obj, opts);
        }

        public static T? FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public static void ToJsonFile<T>(T obj, string filePath)
        {
            var json = ToJson(obj);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public static T? FromJsonFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Vault JSON file not found.", filePath);

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return FromJson<T>(json);
        }

        // -------------------------------------------------------------
        // Specialized Note Serialization
        // -------------------------------------------------------------

        public static void SaveNoteAsMarkdown(MarkdownNote note, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            var safeTitle = string.Join("_", note.Title.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(folderPath, $"{safeTitle}.md");

            var markdown = new StringBuilder()
                .AppendLine($"# {note.Title}")
                .AppendLine()
                .AppendLine(note.Content)
                .ToString();

            File.WriteAllText(filePath, markdown, Encoding.UTF8);
        }

        public static void SaveNotesAsJson(IEnumerable<MarkdownNote> notes, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            var json = ToJson(notes);
            var filePath = Path.Combine(folderPath, "notes.json");
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public static List<MarkdownNote> LoadNotesFromJson(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<MarkdownNote>();

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return FromJson<List<MarkdownNote>>(json) ?? new List<MarkdownNote>();
        }

        // -------------------------------------------------------------
        // Vault Header / Config Serialization
        // -------------------------------------------------------------

        public static void SaveHeader(VaultHeader header, string vaultDirectory)
        {
            Directory.CreateDirectory(vaultDirectory);
            var path = Path.Combine(vaultDirectory, "vault.header.json");
            ToJsonFile(header, path);
        }

        public static VaultHeader? LoadHeader(string vaultDirectory)
        {
            var path = Path.Combine(vaultDirectory, "vault.header.json");
            return File.Exists(path) ? FromJsonFile<VaultHeader>(path) : null;
        }

        public static void SaveConfig(VaultInstanceConfig config, string vaultDirectory)
        {
            var path = Path.Combine(vaultDirectory, "vault.config.json");
            ToJsonFile(config, path);
        }

        public static VaultInstanceConfig? LoadConfig(string vaultDirectory)
        {
            var path = Path.Combine(vaultDirectory, "vault.config.json");
            return File.Exists(path) ? FromJsonFile<VaultInstanceConfig>(path) : null;
        }

        // -------------------------------------------------------------
        // Version Tagging
        // -------------------------------------------------------------

        public static string GetSerializationVersion() => "1.0.0";
    }
}
