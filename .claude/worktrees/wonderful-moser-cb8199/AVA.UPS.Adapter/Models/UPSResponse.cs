using System.Collections.Generic;

namespace AVA.UPS.Adapter.Models
{
    /// <summary>
    /// Standardized response from any module or transport layer.
    /// Always wraps List<UParam>, even for errors.
    /// </summary>
    public class UPSResponse
    {
        public bool Success { get; set; }
        public List<UParam> Payload { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Lightweight outbound payload model used by the UI services.
    /// </summary>
    public class UPSPayload
    {
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string FormatHint { get; set; } = "text/plain";
    }
}
