// ─────────────────────────────────────────────────────────────────────────────
//  Class    : RemoteConnectionProfile
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Connection profile for a remotely hosted AVA core or UPS API.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    public class RemoteConnectionProfile : ConnectionProfile
    {
        /// <summary>
        /// Bearer token for authenticated remote endpoints.
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// When true, uses HTTPS instead of HTTP.
        /// </summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>
        /// Optional API path prefix e.g. /api/v1
        /// </summary>
        public string BasePath { get; set; } = string.Empty;
    }
}