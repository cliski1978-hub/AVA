// ─────────────────────────────────────────────────────────────────────────────
//  Class    : AppSettings
//  Namespace: AVA.UI.CORE.Models.Settings
//  Purpose  : Root application settings container. Persisted to disk as JSON
//             by AvaSettingsService.
// ─────────────────────────────────────────────────────────────────────────────

using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Models.Settings
{
    public class AppSettings
    {
        public List<LocalConnectionProfile> LocalProfiles { get; set; } = new();
        public List<RemoteConnectionProfile> RemoteProfiles { get; set; } = new();
        public List<AgentConnectionProfile> AgentProfiles { get; set; } = new();
        public List<LLMProfile> LLMProfiles { get; set; } = new();
        public List<LogSettings> LogSettings { get; set; } = new();
        public List<VaultState> Vaults { get; set; } = new();

        public string SelectedProfileName { get; set; } = string.Empty;
        public string SelectedLLMProfileName { get; set; } = string.Empty;

        /// <summary>
        /// When true, bypasses legacy core and connects directly
        /// to configured LLM endpoints.
        /// </summary>
        public bool UseDirectEndpoints { get; set; } = true;

        /// <summary>
        /// When true, uses mock data instead of live services.
        /// </summary>
        public bool UseMockCore { get; set; } = true;

        /// <summary>
        /// Persisted dock layout state.
        /// Saves panel positions, sizes, and visibility across sessions.
        /// </summary>
        public DockLayoutState DockLayout { get; set; } = DockLayoutDefaults.Default();

        /// <summary>
        /// Persisted canvas session state — card positions, types, and layout.
        /// </summary>
        public SessionState ActiveCanvas { get; set; } = new();
    }
}