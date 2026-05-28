// ─────────────────────────────────────────────────────────────────────────────
//  Class    : LocalConnectionProfile
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Connection profile for a locally running AVA core or LLM process.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    public class LocalConnectionProfile : ConnectionProfile
    {
        /// <summary>
        /// Full path to the core executable.
        /// </summary>
        public string CorePath { get; set; } = string.Empty;

        /// <summary>
        /// Working directory for the launched process.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Command line arguments passed on launch.
        /// </summary>
        public string LaunchArgs { get; set; } = string.Empty;

        /// <summary>
        /// When true, automatically launches the process on connect.
        /// </summary>
        public bool LaunchOnConnect { get; set; } = false;

        /// <summary>
        /// Milliseconds to wait after launch before attempting connection.
        /// </summary>
        public int LaunchDelayMs { get; set; } = 1000;
    }
}