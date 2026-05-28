using System.Text.Json.Serialization;

namespace AVA.Vault.Core.Config
{
    /// <summary>
    /// Defines configuration settings for the Vault registry subsystem.
    /// Controls registry mode, synchronization, and persistence behavior.
    /// </summary>
    public sealed class VaultRegistryConfig
    {
        [JsonPropertyName("mode")]
        public VaultRegistryMode Mode { get; set; } = VaultRegistryMode.Hybrid;

        [JsonPropertyName("registryPath")]
        public string RegistryPath { get; set; } = "./Registry";

        [JsonPropertyName("remoteEndpoint")]
        public string? RemoteEndpoint { get; set; }

        [JsonPropertyName("syncIntervalSeconds")]
        public int SyncIntervalSeconds { get; set; } = 60;

        [JsonPropertyName("enableAutoSync")]
        public bool EnableAutoSync { get; set; } = true;

        [JsonPropertyName("allowAnonymousVaults")]
        public bool AllowAnonymousVaults { get; set; } = false;

        [JsonPropertyName("defaultSourceKind")]
        public string DefaultSourceKind { get; set; } = "Sqlite";

        [JsonPropertyName("identityNodeId")]
        public string IdentityNodeId { get; set; } = "node-localhost";

        [JsonPropertyName("logRegistryActivity")]
        public bool LogRegistryActivity { get; set; } = true;
    }

    public enum VaultRegistryMode
    {
        Local,
        Remote,
        Hybrid,
        Database
    }
}
