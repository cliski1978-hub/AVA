using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;

namespace Ava.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Minimal deterministic embedding provider for tests.
    /// Produces pseudo-random float vectors based on text hash.
    /// </summary>
    public sealed class TestEmbeddingProvider : IEmbeddingProvider
    {
        private const int Dimension = 64; // small fixed vector length for test speed

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));

            var hash = BitConverter.ToUInt32(
                System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(text))
                    .Take(4).ToArray());

            var random = new Random(unchecked((int)hash));
            var vector = new float[Dimension];
            for (int i = 0; i < Dimension; i++)
                vector[i] = (float)(random.NextDouble() * 2 - 1); // range [-1, 1]

            return Task.FromResult(vector);
        }

        public int GetEmbeddingDimension() => Dimension;
    }
}
