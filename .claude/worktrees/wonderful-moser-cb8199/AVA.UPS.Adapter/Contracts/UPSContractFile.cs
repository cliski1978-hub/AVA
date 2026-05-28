using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Contracts
{
    /// <summary>
    /// Root object for each module's JSON contract file.
    /// </summary>
    public class UPSContractFile
    {
        [JsonPropertyName("module")]
        public string Module { get; set; } = default!;

        [JsonPropertyName("version")]
        public string Version { get; set; } = UPSProtocol.Version;

        [JsonPropertyName("methods")]
        public List<UPSMethodContract> Methods { get; set; } = new();

        // Optional future-proof metadata
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        [JsonPropertyName("updated")]
        public string? Updated { get; set; }

        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }
    }
}
