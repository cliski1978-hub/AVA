using System.Threading.Tasks;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Defines the minimal contract required for an AVA plugin to register with the plugin host.
    /// </summary>
    public interface IAVAPlugin
    {
        /// <summary>
        /// Gets the immutable plugin manifest describing identity, capabilities, and render information.
        /// </summary>
        AVAPluginManifest Manifest { get; }

        /// <summary>
        /// Initializes the plugin using the shared AVA plugin context.
        /// </summary>
        /// <param name="context">The context containing host services and application state.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        Task InitializeAsync(AVAPluginContext context);
    }
}
