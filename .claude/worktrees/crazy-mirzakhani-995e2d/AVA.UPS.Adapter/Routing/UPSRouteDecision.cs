using System;

namespace AVA.UPS.Adapter.Routing
{
    /// <summary>
    /// The resolved route for a UPS message.
    /// Includes target module, transport, endpoint, and timestamp.
    /// </summary>
    public class UPSRouteDecision
    {
        public string Target { get; set; } = string.Empty;
        public string Transport { get; set; } = string.Empty;
        public string? Endpoint { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
