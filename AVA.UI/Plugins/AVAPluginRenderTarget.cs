namespace AVA.UI.Plugins
{
    /// <summary>
    /// Describes where a plugin can be mounted within the AVA user interface.
    /// </summary>
    public enum AVAPluginRenderTarget
    {
        /// <summary>
        /// The plugin is rendered as a full page experience.
        /// </summary>
        Page = 0,

        /// <summary>
        /// The plugin is rendered inside a persistent panel surface.
        /// </summary>
        Panel,

        /// <summary>
        /// The plugin is rendered inside a modal or dialog surface.
        /// </summary>
        Modal,

        /// <summary>
        /// The plugin is rendered inside a docked workspace region.
        /// </summary>
        Dock,

        /// <summary>
        /// The plugin is rendered onto a canvas-like composition surface.
        /// </summary>
        CanvasSurface
    }
}
