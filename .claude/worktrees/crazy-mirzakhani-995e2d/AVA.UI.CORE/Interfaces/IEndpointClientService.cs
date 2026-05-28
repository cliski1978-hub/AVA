using AVA.UI.CORE.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVA.UI.CORE.Interfaces
{
    public interface IEndpointClientService
    {
        /// <summary>
        /// Validates connectivity to the target endpoint described by the given profile.
        /// For HTTP, performs a HEAD/GET probe; for others, a minimal handshake (future).
        /// </summary>
        /// <param name="profile">A CORE connection profile (Local/Remote/Agent).</param>
        /// <param name="selectedLLM">Optional LLM profile (for direct LLM mode payload hints).</param>
        Task<bool> ConnectAsync(ConnectionProfile profile, LLMProfile selectedLLM = null, CancellationToken ct = default);

        /// <summary>
        /// Sends a plain text prompt to the configured endpoint and returns the raw response body.
        /// The service determines the correct wire format based on the profile (Agent vs LLM).
        /// </summary>
        Task<EndpointResult> SendTextAsync(string text, ConnectionProfile profile, LLMProfile selectedLLM = null, CancellationToken ct = default);

        /// <summary>
        /// Sends an arbitrary JSON payload (already serialized) to the endpoint and returns raw response.
        /// Use this when the caller has a specific schema (e.g., /v1/chat/completions).
        /// </summary>
        Task<EndpointResult> SendJsonAsync(string jsonPayload, ConnectionProfile profile, LLMProfile selectedLLM = null, CancellationToken ct = default);
    }

    /// <summary>
    /// Minimal, transport-agnostic result for endpoint calls.
    /// </summary>
    public sealed class EndpointResult
    {
        public bool Success { get; }
        public int StatusCode { get; }
        public string Body { get; }
        public string Error { get; }

        public EndpointResult(bool success, int statusCode, string body, string error = "")
        {
            Success = success;
            StatusCode = statusCode;
            Body = body ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public static EndpointResult Ok(int statusCode, string body) =>
            new EndpointResult(true, statusCode, body);

        public static EndpointResult Fail(int statusCode, string error, string body = "") =>
            new EndpointResult(false, statusCode, body, error);
    }
}
