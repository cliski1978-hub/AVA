namespace AVA.Memory.Abstractions
{
    public interface IEmbeddingProvider
    {
        Task<float[]> EmbedAsync(string text, CancellationToken ct);
    }
}