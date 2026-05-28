// ─────────────────────────────────────────────────────────────────────────────
//  Class     : DockPanelState
//  Namespace : AVA.UI.CORE.Models.UI
//  Purpose   : Persisted state for a single dockable panel.
//              Saved to settings.json via DockLayoutState.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Persisted state for a single dockable panel.
    /// </summary>
    public class DockPanelState
    {
        /// <summary>Unique panel identifier. Must match a PanelId constant.</summary>
        public string PanelId { get; set; } = string.Empty;

        /// <summary>Which dock zone this panel lives in.</summary>
        public string ZoneId { get; set; } = DockZoneId.Center;

        /// <summary>Display order within the zone. Lower = first.</summary>
        public int Order { get; set; } = 0;

        /// <summary>Whether the panel is collapsed.</summary>
        public bool IsCollapsed { get; set; } = false;

        /// <summary>Whether the panel is floating (detached from zone).</summary>
        public bool IsFloating { get; set; } = false;

        /// <summary>Whether the panel is visible at all.</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>X position when floating (pixels from left).</summary>
        public double FloatX { get; set; } = 100;

        /// <summary>Y position when floating (pixels from top).</summary>
        public double FloatY { get; set; } = 100;

        /// <summary>Width when floating.</summary>
        public double FloatWidth { get; set; } = 480;

        /// <summary>Height when floating.</summary>
        public double FloatHeight { get; set; } = 400;
    }
}
