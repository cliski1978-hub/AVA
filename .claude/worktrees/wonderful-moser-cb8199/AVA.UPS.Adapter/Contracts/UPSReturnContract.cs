using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Contracts
{
    /// <summary>
    /// Describes the full expected return schema of a UPS method.
    /// This is loaded from external contract definition files.
    /// </summary>
    public class UPSReturnContract
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        // Example: true if the method returns a list of items
        [JsonPropertyName("isList")]
        public bool IsList { get; set; } = false;

        // Structured return parameters (same structure as method parameters)
        [JsonPropertyName("fields")]
        public List<UPSParameterContract>? Fields { get; set; }

        // Human-readable description of what is returned
        [JsonPropertyName("description")]
        public string? Description { get; set; }

    }
}
