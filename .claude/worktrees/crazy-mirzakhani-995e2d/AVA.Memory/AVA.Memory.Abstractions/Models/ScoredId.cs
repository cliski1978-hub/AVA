namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a scored vector search result.
    /// </summary>
    public sealed class ScoredId
    {
        /// <summary>The record ID that matched the query.</summary>
        public string ID { get; set; }

        /// <summary>The similarity or relevance score of the result (higher = more similar).</summary>
        public float Score { get; set; }
    }
}
