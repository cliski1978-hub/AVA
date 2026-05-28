// ─────────────────────────────────────────────────────────────────────────────
//  Class     : DockLayoutDefaults
//  Namespace : AVA.UI.CORE.Models.UI
//  Purpose   : Defines the default dock layout for AVA on first run.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Default dock layout factory.
    /// </summary>
    public static class DockLayoutDefaults
    {
        /// <summary>Current layout schema version. Bump when defaults change incompatibly.</summary>
        public const int CurrentVersion = 3;

        /// <summary>
        /// Returns the default AVA layout:
        ///   Top    — ModelPicker + SessionTabs (always visible)
        ///   Center — ChatOutput, MemoryLog, ReflectionPane, SettingsPane
        ///            (tab-switched — only the active tab's panel shows)
        ///   Bottom — ChatInput (always visible)
        ///   Float  — PromptPreview (hidden by default)
        /// </summary>
        public static DockLayoutState Default()
        {
            return new DockLayoutState
            {
                Version          = CurrentVersion,
                LeftCollapsed    = true,
                RightCollapsed   = true,
                BottomCollapsed  = false,
                BottomZoneHeight = 160,
                Panels = new Dictionary<string, DockPanelState>
                {
                    [PanelId.ModelPicker] = new()
                    {
                        PanelId     = PanelId.ModelPicker,
                        ZoneId      = DockZoneId.Top,
                        Order       = 0,
                        IsVisible   = false,
                        IsCollapsed = true
                    },
                    [PanelId.SessionTabs] = new()
                    {
                        PanelId     = PanelId.SessionTabs,
                        ZoneId      = DockZoneId.Top,
                        Order       = 1,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.ChatOutput] = new()
                    {
                        PanelId     = PanelId.ChatOutput,
                        ZoneId      = DockZoneId.Center,
                        Order       = 0,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.MemoryLog] = new()
                    {
                        PanelId     = PanelId.MemoryLog,
                        ZoneId      = DockZoneId.Center,
                        Order       = 1,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.Reflection] = new()
                    {
                        PanelId     = PanelId.Reflection,
                        ZoneId      = DockZoneId.Center,
                        Order       = 2,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.Settings] = new()
                    {
                        PanelId     = PanelId.Settings,
                        ZoneId      = DockZoneId.Center,
                        Order       = 3,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.ChatInput] = new()
                    {
                        PanelId     = PanelId.ChatInput,
                        ZoneId      = DockZoneId.Bottom,
                        Order       = 0,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.Canvas] = new()
                    {
                        PanelId     = PanelId.Canvas,
                        ZoneId      = DockZoneId.Center,
                        Order       = 4,
                        IsVisible   = true,
                        IsCollapsed = false
                    },
                    [PanelId.PromptPreview] = new()
                    {
                        PanelId     = PanelId.PromptPreview,
                        ZoneId      = DockZoneId.FloatLayer,
                        Order       = 0,
                        IsVisible   = false,
                        IsFloating  = true,
                        FloatX      = 200,
                        FloatY      = 100,
                        FloatWidth  = 400,
                        FloatHeight = 300
                    }
                }
            };
        }
    }
}
