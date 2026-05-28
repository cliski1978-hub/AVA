using System.Collections.Generic;

namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Data sent by a module initiating a UPS handshake to another module.
    /// </summary>
    public class UPSHandshakeRequest
    {
        public string ProtocolVersion { get; set; } = "1.0.0";
        public UPSHandshakeIdentity Identity { get; set; } = new();
        public UPSHandshakeCapabilities Capabilities { get; set; } = new();
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
