namespace AVA.UI.CORE.Models.Settings
{
    /// <summary>
    /// Provider/platform connection profile. Owns transport and authentication only.
    /// </summary>
    public class ProviderProfile
    {
        /// <summary>Stable provider profile identifier.</summary>
        public string ProviderProfileId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Friendly provider profile name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Provider/platform type, such as Ollama, Venice, or Custom.</summary>
        public string ProviderType { get; set; } = string.Empty;

        /// <summary>Custom provider type when ProviderType is Custom.</summary>
        public string CustomProviderType { get; set; } = string.Empty;

        /// <summary>Transport protocol type, such as Http, WebSocket, Grpc, or Custom.</summary>
        public string TransportType { get; set; } = "Http";

        /// <summary>Custom transport type when TransportType is Custom.</summary>
        public string CustomTransportType { get; set; } = string.Empty;

        /// <summary>Base endpoint URL for this provider.</summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>Temporary compatibility auth value. Future Vault integration should replace this with a secret reference.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Temporary compatibility secondary secret. Future Vault integration should replace this with a secret reference.</summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>Optional raw custom headers, one key-value pair per line.</summary>
        public string CustomHeadersAsText { get; set; } = string.Empty;

        /// <summary>Optional request timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>Optional retry count for provider transport.</summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>Whether this provider supports streaming responses.</summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>Provider metadata and compatibility annotations.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
