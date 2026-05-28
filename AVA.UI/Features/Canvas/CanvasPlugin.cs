using System.Threading.Tasks;
using AVA.UI.Plugins;

namespace AVA.UI.Features.Canvas
{
    /// <summary>
    /// Provides the Canvas document editing surface as an AVA plugin.
    /// </summary>
    public sealed class CanvasPlugin : IAVAPlugin
    {
        private AVAPluginContext? _context;

        /// <summary>
        /// Gets the Canvas plugin manifest.
        /// </summary>
        public AVAPluginManifest Manifest { get; } = new()
        {
            PluginId = "ava.canvas",
            DisplayName = "Canvas",
            Description = "Document viewer and editor surface for AVA-generated content.",
            Version = "1.0.0",
            RenderTarget = AVAPluginRenderTarget.Page,
            RenderComponent = "AVA.UI.Features.Canvas",
            Capabilities =
            {
                AVAPluginCapability.CanvasDocumentEditor
            }
        };

        /// <summary>
        /// Initializes the Canvas plugin with the shared AVA plugin context.
        /// </summary>
        /// <param name="context">The plugin initialization context.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public Task InitializeAsync(AVAPluginContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }
    }
}
