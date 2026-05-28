namespace AVA.Memory.Abstractions.Models.VectorDB
{
    public class VectorDBConfigOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int Dimension { get; set; } = 1536;
        public string Metric { get; set; } = "cosine";
        public string DefaultCollection { get; set; } = "runtime_memory";
    }
}
