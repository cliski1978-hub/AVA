// ─────────────────────────────────────────────────────────────────────────────
//  Class     : DockLayoutService
//  Namespace : AVA.UI.CORE.Services
//  Purpose   : Manages dock layout state at runtime.
//              Provides panel move, resize, collapse, and float operations.
//              Persists layout changes to AppSettings via AvaSettingsService.
// ─────────────────────────────────────────────────────────────────────────────

using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Runtime manager for dock layout state.
    /// All dock operations route through here — nothing mutates layout directly.
    /// </summary>
    public class DockLayoutService
    {
        private readonly AvaSettingsService _Settings;

        /// <summary>Fires whenever layout changes so components can re-render.</summary>
        public event Action? OnLayoutChanged;

        private void Notify() => OnLayoutChanged?.Invoke();

        /// <summary>
        /// Initializes the service with the settings service.
        /// Resets layout to defaults if the saved version is outdated.
        /// </summary>
        public DockLayoutService(AvaSettingsService Settings)
        {
            _Settings = Settings;
            EnsureVersion();
        }

        /// <summary>
        /// Resets layout to defaults if the persisted version is behind the current schema.
        /// </summary>
        private void EnsureVersion()
        {
            if (_Settings.AppSettings.DockLayout.Version < DockLayoutDefaults.CurrentVersion)
            {
                _Settings.AppSettings.DockLayout = DockLayoutDefaults.Default();
                Save();
            }
        }

        /// <summary>Current layout state.</summary>
        public DockLayoutState Layout => _Settings.AppSettings.DockLayout;

        // ── Panel Operations ──────────────────────────────────────────────────

        /// <summary>Moves a panel to a different dock zone.</summary>
        public void MovePanel(string PanelId, string TargetZoneId)
        {
            var Panel        = Layout.GetOrCreate(PanelId);
            Panel.ZoneId     = TargetZoneId;
            Panel.IsFloating = TargetZoneId == DockZoneId.FloatLayer;
            Save();
            Notify();
        }

        /// <summary>Collapses or expands a panel.</summary>
        public void SetCollapsed(string PanelId, bool Collapsed)
        {
            Layout.GetOrCreate(PanelId).IsCollapsed = Collapsed;
            Save();
            Notify();
        }

        /// <summary>Toggles a panel's collapsed state.</summary>
        public void ToggleCollapsed(string PanelId)
        {
            var Panel         = Layout.GetOrCreate(PanelId);
            Panel.IsCollapsed = !Panel.IsCollapsed;
            Save();
            Notify();
        }

        /// <summary>Shows or hides a panel entirely.</summary>
        public void SetVisible(string PanelId, bool Visible)
        {
            Layout.GetOrCreate(PanelId).IsVisible = Visible;
            Save();
            Notify();
        }

        /// <summary>Detaches a panel into a floating window.</summary>
        public void FloatPanel(string PanelId, double X = 100, double Y = 100,
            double Width = 480, double Height = 400)
        {
            var Panel         = Layout.GetOrCreate(PanelId);
            Panel.ZoneId      = DockZoneId.FloatLayer;
            Panel.IsFloating  = true;
            Panel.FloatX      = X;
            Panel.FloatY      = Y;
            Panel.FloatWidth  = Width;
            Panel.FloatHeight = Height;
            Panel.IsVisible   = true;
            Save();
            Notify();
        }

        /// <summary>Docks a floating panel back into a zone.</summary>
        public void DockPanel(string PanelId, string TargetZoneId)
        {
            var Panel        = Layout.GetOrCreate(PanelId);
            Panel.ZoneId     = TargetZoneId;
            Panel.IsFloating = false;
            Save();
            Notify();
        }

        /// <summary>Updates the float position of a panel during drag (no save).</summary>
        public void UpdateFloatPosition(string PanelId, double X, double Y)
        {
            var Panel    = Layout.GetOrCreate(PanelId);
            Panel.FloatX = X;
            Panel.FloatY = Y;
            Notify();
        }

        /// <summary>Saves float position after drag ends.</summary>
        public void SaveFloatPosition(string PanelId, double X, double Y)
        {
            var Panel    = Layout.GetOrCreate(PanelId);
            Panel.FloatX = X;
            Panel.FloatY = Y;
            Save();
            Notify();
        }

        // ── Zone Operations ───────────────────────────────────────────────────

        /// <summary>Sets left zone collapsed state.</summary>
        public void SetLeftCollapsed(bool Collapsed)
        {
            Layout.LeftCollapsed = Collapsed;
            Save();
            Notify();
        }

        /// <summary>Sets right zone collapsed state.</summary>
        public void SetRightCollapsed(bool Collapsed)
        {
            Layout.RightCollapsed = Collapsed;
            Save();
            Notify();
        }

        /// <summary>Sets bottom zone collapsed state.</summary>
        public void SetBottomCollapsed(bool Collapsed)
        {
            Layout.BottomCollapsed = Collapsed;
            Save();
            Notify();
        }

        /// <summary>Resets layout to defaults.</summary>
        public void ResetToDefaults()
        {
            _Settings.AppSettings.DockLayout = DockLayoutDefaults.Default();
            Save();
            Notify();
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>Returns all visible panels in a specific zone, ordered by Order.</summary>
        public IEnumerable<DockPanelState> GetPanelsInZone(string ZoneId)
        {
            return Layout.Panels.Values
                .Where(P => P.ZoneId == ZoneId && P.IsVisible)
                .OrderBy(P => P.Order);
        }

        /// <summary>Returns the state for a specific panel.</summary>
        public DockPanelState GetPanel(string PanelId)
        {
            return Layout.GetOrCreate(PanelId);
        }

        // ── Persistence ───────────────────────────────────────────────────────

        private void Save() => _Settings.SaveSettings();
    }
}
