using System;
using System.Collections.Generic;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Represents the metadata and UI mounting information for an AVA plugin.
    /// </summary>
    public sealed class AVAPluginManifest
    {
        /// <summary>
        /// Gets or sets the unique plugin identifier.
        /// </summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name shown in the AVA user interface.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable plugin description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin version string.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the capabilities advertised by the plugin.
        /// </summary>
        public List<AVAPluginCapability> Capabilities { get; set; } = new();

        /// <summary>
        /// Gets or sets the preferred AVA surface used to render the plugin.
        /// </summary>
        public AVAPluginRenderTarget RenderTarget { get; set; } = AVAPluginRenderTarget.Page;

        /// <summary>
        /// Gets or sets the component identity or route key used by the UI host to render the plugin.
        /// </summary>
        public string RenderComponent { get; set; } = string.Empty;
    }
}
