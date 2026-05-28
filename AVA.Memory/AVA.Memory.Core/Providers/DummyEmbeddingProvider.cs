using System;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;   // make sure this matches the interface namespace

namespace AVA.Memory.Core.Providers
{
    /// <summary>
    /// Simple test embedding provider that returns a deterministic pseudo-vector.
    /// </summary>
    public class DummyEmbeddingProvider : IEmbeddingProvider
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Input text cannot be null or empty.", nameof(text));

            // Example: return a simple pseudo-random vector based on text hash
            const int dimension = 768;
            var vector = new float[dimension];
            var random = new Random(text.GetHashCode());

            for (int i = 0; i < dimension; i++)
                vector[i] = (float)(random.NextDouble() * 2 - 1);

            return Task.FromResult(vector);
        }
    }
}
