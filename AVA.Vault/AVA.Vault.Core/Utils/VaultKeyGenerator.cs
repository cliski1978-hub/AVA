using System;
using System.Security.Cryptography;
using System.Text;

namespace AVA.Vault.Core.Utils
{
    /// <summary>
    /// Provides deterministic and collision-resistant key generation for vault entities.
    /// Keys encode hierarchical context (source, user, agent, vault, type, baseId, version).
    /// </summary>
    public static class VaultKeyGenerator
    {
        /// <summary>
        /// Generates a structured Vault key using the canonical format:
        /// <para><c>{SourceID}:{UserID}:{AgentID}:{VaultID}:{TypeID}:{BaseID}[:v{version}]</c></para>
        /// Example: <c>memory:neo:ava:vlt1234:note:9f0a3b21:v2</c>
        /// </summary>
        public static string GenerateKey(
            string sourceId,
            string userId,
            string agentId,
            string vaultId,
            string typeId,
            string baseInput,
            int version = 1,
            bool compactHash = true)
        {
            if (string.IsNullOrWhiteSpace(baseInput))
                throw new ArgumentException("Base input cannot be null or empty.", nameof(baseInput));

            // Generate hash segment
            var hash = ComputeHash(baseInput, compactHash ? 8 : 20);

            var versionSegment = version > 1 ? $":v{version}" : string.Empty;

            return $"{sourceId}:{userId}:{agentId}:{vaultId}:{typeId}:{hash}{versionSegment}".ToLower();
        }

        /// <summary>
        /// Generates a short unique ID (e.g., for transient or mock vaults).
        /// </summary>
        public static string GenerateShortKey(string prefix = "vlt")
        {
            var guidBytes = Guid.NewGuid().ToByteArray();
            var shortHash = Convert.ToBase64String(guidBytes)
                .Replace("=", string.Empty)
                .Replace("+", string.Empty)
                .Replace("/", string.Empty)
                .Substring(0, 10)
                .ToLower();

            return $"{prefix}-{shortHash}";
        }

        /// <summary>
        /// Creates a deterministic hash of a string with the given length.
        /// </summary>
        private static string ComputeHash(string input, int length)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hex = Convert.ToHexString(bytes).ToLower();
            return hex.Substring(0, Math.Min(length, hex.Length));
        }
    }
}
