// ─────────────────────────────────────────────────────────────────────────────
//  Class     : DockLayoutState
//  Namespace : AVA.UI.CORE.Models.UI
//  Purpose   : Root layout state. Persisted to settings.json.
//              Contains all panel states and zone sizes.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Root dock layout state. Persisted to settings.json.
    /// </summary>
    public class DockLayoutState
    {
        /// <summary>
        /// Layout schema version. Increment when defaults change incompatibly
        /// so stale saved layouts are automatically reset on next load.
        /// </summary>
        public int Version { get; set; } = 0;

        /// <summary>All panel states keyed by PanelId.</summary>
        public Dictionary<string, DockPanelState> Panels { get; set; } = new();

        /// <summary>Left zone width in pixels.</summary>
        public double LeftZoneWidth { get; set; } = 280;

        /// <summary>Right zone width in pixels.</summary>
        public double RightZoneWidth { get; set; } = 280;

        /// <summary>Bottom zone height in pixels.</summary>
        public double BottomZoneHeight { get; set; } = 200;

        /// <summary>Top zone height in pixels.</summary>
        public double TopZoneHeight { get; set; } = 40;

        /// <summary>Whether the left zone is collapsed.</summary>
        public bool LeftCollapsed { get; set; } = false;

        /// <summary>Whether the right zone is collapsed.</summary>
        public bool RightCollapsed { get; set; } = false;

        /// <summary>Whether the bottom zone is collapsed.</summary>
        public bool BottomCollapsed { get; set; } = false;

        /// <summary>
        /// Returns the state for a panel, creating a default entry if not found.
        /// </summary>
        public DockPanelState GetOrCreate(string PanelId, string DefaultZone = DockZoneId.Center)
        {
            if (!Panels.ContainsKey(PanelId))
            {
                Panels[PanelId] = new DockPanelState
                {
                    PanelId = PanelId,
                    ZoneId  = DefaultZone
                };
            }
            return Panels[PanelId];
        }
    }
}
