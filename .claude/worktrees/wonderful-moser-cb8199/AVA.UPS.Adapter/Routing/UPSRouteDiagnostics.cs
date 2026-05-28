using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Routing
{
    /// <summary>
    /// Mutable dictionary-style diagnostics object.
    /// Used by ProtocolRouter and UPSRoutingService for tracing routing flow.
    /// </summary>
    public class UPSRouteDiagnostics : Dictionary<string, object>
    {
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public long DurationMs { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        public string? SourceModule { get; set; }
        public string? TargetModule { get; set; }
    }
}
