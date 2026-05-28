using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVA.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Provides safe JSON serialization and deserialization utilities
    /// for test input parsing and structured Excel result output.
    /// </summary>
    internal static class JsonHelper
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>
        /// Attempts to deserialize JSON text into a dictionary (case-insensitive).
        /// Returns an empty dictionary on parse error.
        /// </summary>
        public static Dictionary<string, object> ParseMetadata(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, Options);
                return dict ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Serializes an object to a compact JSON string.
        /// </summary>
        public static string ToJson(object obj)
        {
            if (obj == null)
                return "{}";
            try
            {
                return JsonSerializer.Serialize(obj, Options);
            }
            catch
            {
                return "{}";
            }
        }

        /// <summary>
        /// Formats a dictionary of metadata for display or Excel output.
        /// </summary>
        public static string FormatMetadata(Dictionary<string, object>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return string.Empty;

            try
            {
                return string.Join("; ", metadata.Select(kv => $"{kv.Key}:{kv.Value}"));
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Attempts to parse a simple JSON array of strings or numbers.
        /// Returns null if the format is invalid.
        /// </summary>
        public static List<string>? ParseSimpleArray(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json, Options);
                return list ?? new List<string>();
            }
            catch
            {
                return null;
            }
        }
    }
}
