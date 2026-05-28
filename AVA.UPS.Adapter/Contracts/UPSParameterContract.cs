using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Contracts
{
    /// <summary>
    /// Defines a single expected parameter in a UPS method contract.
    /// </summary>
    public class UPSParameterContract
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = default!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;   // semantic type: string, int, identity, memoryRecord, etc.

        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        [JsonPropertyName("default")]
        public object? Default { get; set; } = null;
    }
}
