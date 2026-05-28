using System.Collections.Generic;

namespace AVA.UPS.Adapter.Models
{
    /// <summary>
    /// Represents a universal parameter used across all AVA modules.
    /// The Type field is semantic: "string", "int", "identity", "memoryRecord",
    /// "embedding", "list<string>", etc. Value can be primitive or structured.
    /// </summary>
    public class UParam
    {
        public string Key { get; set; } = default!;
        public string Type { get; set; } = default!;
        public object? Value { get; set; }
        public Dictionary<string, object>? Meta { get; set; } = new();
    }
}
