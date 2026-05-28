// ─────────────────────────────────────────────────────────────────────────────
//  Class    : LogSettings
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Defines a single log output target for AVA.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    public class LogSettings
    {
        /// <summary>
        /// Display title for this log entry.
        /// </summary>
        public string LogTitle { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this log captures.
        /// </summary>
        public string LogDescription { get; set; } = string.Empty;

        /// <summary>
        /// File path where log output is written.
        /// </summary>
        public string LogFilePath { get; set; } = string.Empty;

        /// <summary>
        /// When true this log target is active.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}