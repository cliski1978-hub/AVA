using System;
using System.Security.Cryptography;
using System.Text;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// Composes a distributed-swarm-safe ID using
    /// timestamp + nodeId + moduleId + processId + sequence + random salt.
    /// </summary>
    internal static class UPSIdComposer
    {
        private const string BASE32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string Compose(string nodeId, string moduleId, string processId, long sequence)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Build raw byte buffer
            var input = $"{timestamp}:{nodeId}:{moduleId}:{processId}:{sequence}:{Guid.NewGuid()}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Convert first 16 bytes into 26-char Base32 swarm ID
            char[] id = new char[26];

            for (int i = 0; i < 26; i++)
            {
                id[i] = BASE32[hash[i] & 31];
            }

            return new string(id);
        }
    }
}
