using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Logging
{
    /// <summary>
    /// Represents the lifecycle trace of a UPSMessageEnvelope,
    /// including routing, dispatch, errors, and timing.
    /// </summary>
    public class UPSMessageTrace
    {
        public string TraceId { get; set; } = Guid.NewGuid().ToString();

        public string EnvelopeId { get; set; } = default!;
        public string? CorrelationId { get; set; }

        public string Source { get; set; } = default!;
        public string Target { get; set; } = default!;
        public string TargetMethod { get; set; } = default!;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public long? DurationMs =>
            CompletedAt.HasValue
                ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
                : null;

        /// <summary>
        /// Transport used for routing (http, mqtt, tcp, etc.)
        /// </summary>
        public string? Transport { get; set; }

        /// <summary>
        /// Diagnostics collected from routing + host + dispatch.
        /// </summary>
        public Dictionary<string, object>? Diagnostics { get; set; }

        /// <summary>
        /// Optional error info captured during any stage.
        /// </summary>
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }
    }
}
