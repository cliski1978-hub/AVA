using System;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Describes a feature capability that an AVA plugin can advertise.
    /// </summary>
    public enum AVAPluginCapability
    {
        /// <summary>
        /// No specific capability has been declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// Supports a structured canvas-based document editing experience.
        /// </summary>
        CanvasDocumentEditor,

        /// <summary>
        /// Supports viewing document content without full editing behavior.
        /// </summary>
        DocumentViewer,

        /// <summary>
        /// Supports generic document editing behavior.
        /// </summary>
        DocumentEditor,

        /// <summary>
        /// Supports rendering tool outputs or execution results.
        /// </summary>
        ToolResultViewer
    }
}
