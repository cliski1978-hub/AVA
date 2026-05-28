using System.Collections.Generic;

namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Response returned by a remote module upon handshake.
    /// </summary>
    public class UPSHandshakeResponse
    {
        public bool Accepted { get; set; }
        public string ProtocolVersion { get; set; } = "1.0.0";
        public UPSHandshakeIdentity Identity { get; set; } = new();
        public UPSHandshakeCapabilities Capabilities { get; set; } = new();
        public UPSHandshakeError? Error { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
