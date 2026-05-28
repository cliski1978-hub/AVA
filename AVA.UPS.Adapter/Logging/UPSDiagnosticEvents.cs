using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Logging
{
    /// <summary>
    /// High-level diagnostic events emitted by routing, dispatching, and host listeners.
    /// Suitable for logging, telemetry, monitoring, and distributed tracing.
    /// </summary>
    public class UPSDiagnosticEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = default!;          // e.g. "Routing.Start", "Dispatch.Error"
        public string? EnvelopeId { get; set; }
        public string? CorrelationId { get; set; }
        public string? SourceModule { get; set; }
        public string? TargetModule { get; set; }
        public string? Method { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Structured diagnostic fields:
        /// adapter info, bytes sent, response time, etc.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        public string? Message { get; set; }  // Optional human-readable
        public string? Severity { get; set; } = "info"; // info|warn|error|fatal
    }
}
