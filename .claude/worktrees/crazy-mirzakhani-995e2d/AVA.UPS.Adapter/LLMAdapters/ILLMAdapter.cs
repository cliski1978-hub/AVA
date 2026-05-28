// ─────────────────────────────────────────────────────────────────────────────
//  Interface : ILLMAdapter
//  Namespace : AVA.UPS.Adapter.LLMAdapters
//  Purpose   : Common contract for all LLM provider adapters.
//              Each adapter owns its HTTP connection, auth, and
//              provider-specific request/response translation.
//              Configuration is always sourced from LLMProfile — nothing
//              is hardcoded.
// ─────────────────────────────────────────────────────────────────────────────

using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.LLMAdapters
{
    /// <summary>
    /// Common contract for all LLM provider adapters in AVA.
    /// Implementations exist for Anthropic (Claude), OpenAI-compatible
    /// endpoints (ChatGPT, DeepSeek, Venice), and Nomi.
    /// </summary>
    public interface ILLMAdapter
    {
        /// <summary>
        /// Unique provider identifier used for routing (e.g. "anthropic", "openai").
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Human-readable display name shown in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Active model identifier (e.g. "claude-sonnet-4-20250514").
        /// Sourced from LLMProfile at runtime.
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Establishes connectivity and verifies the API key is valid.
        /// </summary>
        Task<LLMConnectionResult> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a prompt and returns the provider response wrapped in a UPSResponse.
        /// </summary>
        Task<UPSResponse> SendAsync(UPSPayload payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a lightweight test prompt and measures latency.
        /// </summary>
        Task<LLMTestResult> TestAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases any held resources (HTTP clients, connections).
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Returns the capabilities reported by this provider.
        /// </summary>
        Task<LLMCapabilitySet> GetCapabilitiesAsync();
    }

    /// <summary>
    /// Result of a ConnectAsync call.
    /// </summary>
    public class LLMConnectionResult
    {
        /// <summary>Whether the connection succeeded.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Human-readable status message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>UTC timestamp of the successful connection.</summary>
        public System.DateTime ConnectedAt { get; set; }
    }

    /// <summary>
    /// Result of a TestAsync call.
    /// </summary>
    public class LLMTestResult
    {
        /// <summary>Whether the test prompt succeeded.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Human-readable status or error message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Round-trip latency in milliseconds.</summary>
        public double LatencyMs { get; set; }
    }

    /// <summary>
    /// Capability information returned by the provider.
    /// </summary>
    public class LLMCapabilitySet
    {
        /// <summary>Maximum output tokens supported.</summary>
        public int MaxTokens { get; set; }

        /// <summary>Maximum temperature value accepted.</summary>
        public float MaxTemperature { get; set; }

        /// <summary>Whether streaming responses are supported.</summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>Supported system prompt modes.</summary>
        public System.Collections.Generic.List<string> SystemPromptCapabilities { get; set; } = new();
    }
}
