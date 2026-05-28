using System;
using System.Collections.Generic;
namespace AVA.Memory.Abstractions.Contracts
{
    /// <summary>
    /// Defines parameters for performing a semantic or vector-based memory query.
    /// </summary>
    public sealed class QueryMemoryRequest
    {
        public string Text { get; set; }
        public float[] Embedding { get; set; }
        public int TopK { get; set; } = 10;
        public float MinScore { get; set; } = 0.0f;
        public string[] Tags { get; set; } = new string[0];
        public string? Topic { get; set; }
        public string? Collection { get; set; }

        public Dictionary<string, object> MetadataFilters { get; set; } = new Dictionary<string, object>();
    }
}
