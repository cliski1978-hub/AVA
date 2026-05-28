// ─────────────────────────────────────────────────────────────────────────────
//  Class    : ConnectionProfile
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Base class for all connection profile types.
//             Inherited by Local, Remote and Agent profiles.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    public abstract class ConnectionProfile
    {
        /// <summary>
        /// Friendly display name shown in the UI.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Target hostname or IP address.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Target port number.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Whether this profile is currently selected.
        /// </summary>
        public bool IsSelected { get; set; } = false;
    }
}