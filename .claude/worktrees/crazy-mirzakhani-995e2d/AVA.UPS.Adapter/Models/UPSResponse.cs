using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Models
{
    /// <summary>
    /// Standardized response from any module or transport layer.
    /// Always wraps List&lt;UParam&gt;, even for errors.
    /// </summary>
    public class UPSResponse
    {
        /// <summary>
        /// Whether the request completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Structured UPS payload returned by the provider or module.
        /// </summary>
        public List<UParam> Payload { get; set; } = new();

        /// <summary>
        /// Error message when the request fails.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Convenience string content set directly by LLM adapters.
        /// Avoids callers needing to walk the Payload list for simple text responses.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Model profile ID that produced this response.
        /// Set by LLM adapters so broadcast callers can correlate responses to models.
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// Raw provider response body captured for diagnostics.
        /// </summary>
        public string? ProviderResponse { get; set; }

        /// <summary>
        /// UTC timestamp when the provider response was received.
        /// </summary>
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>
    /// Lightweight outbound payload model used by the UI services.
    /// </summary>
    public class UPSPayload
    {
        /// <summary>
        /// Request content to send to the target model.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional request headers or routing hints.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Expected content format.
        /// </summary>
        public string FormatHint { get; set; } = "text/plain";
    }
}
