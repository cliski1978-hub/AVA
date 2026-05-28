using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Logging
{
    /// <summary>
    /// Transport-level transmission log for a single UPS request/response pair.
    /// Captures metadata useful for analytics, reliability testing, and debugging.
    /// </summary>
    public class UPSTransmissionLog
    {
        public string TransmissionId { get; set; } = Guid.NewGuid().ToString();

        public string Transport { get; set; } = default!;   // http, tcp, mqtt, etc.
        public string? Endpoint { get; set; }               // URL, queue, topic, etc.

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReceivedAt { get; set; }

        public long? DurationMs =>
            ReceivedAt.HasValue
                ? (long)(ReceivedAt.Value - SentAt).TotalMilliseconds
                : null;

        public int BytesSent { get; set; }
        public int BytesReceived { get; set; }

        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional structured metrics: retries, adapter timings, etc.
        /// </summary>
        public Dictionary<string, object>? Metrics { get; set; }
    }
}
