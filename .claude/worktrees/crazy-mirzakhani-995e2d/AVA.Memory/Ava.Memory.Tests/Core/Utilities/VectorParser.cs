using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace AVA.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Provides safe parsing and serialization for numeric vectors.
    /// Handles CSV, JSON, and whitespace-separated formats.
    /// </summary>
    internal static class VectorParser
    {
        /// <summary>
        /// Parses a vector from a string representation.
        /// Accepts CSV ("0.1,0.2"), JSON ("[0.1,0.2]"), or whitespace ("0.1 0.2").
        /// Returns null if parsing fails.
        /// </summary>
        public static float[]? Parse(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();

            try
            {
                // JSON array format
                if (input.StartsWith("[") && input.EndsWith("]"))
                {
                    var arr = JsonSerializer.Deserialize<List<float>>(input);
                    return arr?.ToArray();
                }

                // CSV or space-separated
                var parts = input
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty)
                    .Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

                var floats = parts
                    .Select(p => float.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
                        ? f
                        : float.NaN)
                    .Where(f => !float.IsNaN(f))
                    .ToArray();

                return floats.Length > 0 ? floats : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes a float array to compact JSON (e.g., [0.1,0.2,0.3]).
        /// </summary>
        public static string ToJson(float[]? vector)
        {
            if (vector == null || vector.Length == 0)
                return "[]";

            try
            {
                return JsonSerializer.Serialize(vector, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch
            {
                return "[]";
            }
        }

        /// <summary>
        /// Converts a float array to a CSV string with configurable precision.
        /// </summary>
        public static string ToCsv(float[]? vector, int decimals = 4)
        {
            if (vector == null || vector.Length == 0)
                return string.Empty;

            return string.Join(",", vector.Select(v => v.ToString($"F{decimals}", CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Generates a deterministic test vector from a string seed for mock embeddings.
        /// </summary>
        public static float[] FromSeed(string seed, int dimensions = 8)
        {
            var floats = new float[dimensions];
            var hash = seed.GetHashCode();
            var rand = new Random(hash);

            for (int i = 0; i < dimensions; i++)
                floats[i] = (float)(rand.NextDouble() * 2 - 1); // range [-1, 1]

            return floats;
        }

        /// <summary>
        /// Computes cosine similarity between two vectors.
        /// Returns 0 if invalid input.
        /// </summary>
        public static float Cosine(float[]? a, float[]? b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0)
                return 0f;

            int len = Math.Min(a.Length, b.Length);
            double dot = 0, magA = 0, magB = 0;

            for (int i = 0; i < len; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0) return 0f;
            return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB)));
        }
    }
}
