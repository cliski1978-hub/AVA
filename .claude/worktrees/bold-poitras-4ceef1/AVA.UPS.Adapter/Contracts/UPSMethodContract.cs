using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Contracts
{
    /// <summary>
    /// Represents a contract for a single method in a module.
    /// Contains its name, version, expected parameters, and return type.
    /// </summary>
    public class UPSMethodContract
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("parameters")]
        public List<UPSParameterContract> Parameters { get; set; } = new();

        [JsonPropertyName("returns")]
        public UPSReturnContract? Return { get; set; }
    }
}
