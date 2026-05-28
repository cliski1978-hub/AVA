// ─────────────────────────────────────────────────────────────────────────────
//  Class    : AgentConnectionProfile
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Connection profile for agent-based AVA endpoints.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    public class AgentConnectionProfile : ConnectionProfile
    {
        /// <summary>
        /// Unique identifier for the target agent.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Full endpoint URL e.g. http://localhost:8080/api/agent
        /// </summary>
        public string EndpointUrl { get; set; } = string.Empty;

        /// <summary>
        /// Protocol — Http, WebSocket, Grpc
        /// </summary>
        public string Protocol { get; set; } = "Http";

        /// <summary>
        /// Optional relay routing info.
        /// </summary>
        public string RelayInfo { get; set; } = string.Empty;

        /// <summary>
        /// When true, routes to a mock endpoint instead of live.
        /// </summary>
        public bool UseMock { get; set; } = false;

        /// <summary>
        /// Optional headers as raw text (key=value, one per line).
        /// </summary>
        public string HeadersAsText { get; set; } = string.Empty;
    }
}