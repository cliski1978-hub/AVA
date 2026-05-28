namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Capability declaration exchanged by modules during handshake.
    /// Indicates supported transports, encodings, compression, etc.
    /// </summary>
    public class UPSHandshakeCapabilities
    {
        public bool SupportsCompression { get; set; } = true;
        public bool SupportsEncryption { get; set; } = false;
        public bool SupportsAsyncResponse { get; set; } = false;
        public bool SupportsStreaming { get; set; } = false;
        public bool SupportsBatching { get; set; } = true;

        /// <summary>
        /// Transport types and encodings supported by this module.
        /// Example: ["http", "grpc", "file", "s3"]
        /// </summary>
        public List<string> SupportedTransports { get; set; } = new();

        /// <summary>
        /// Maximum allowed payload size in bytes.
        /// </summary>
        public long MaxPayloadBytes { get; set; } = 20_000_000; // 20 MB default
    }
}
