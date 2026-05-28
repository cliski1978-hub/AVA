using System.Collections.Generic;
using System.Threading.Tasks;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Defines the runtime registry contract used to register and discover AVA plugins.
    /// </summary>
    public interface IAVAPluginRegistry
    {
        /// <summary>
        /// Gets all registered plugins.
        /// </summary>
        /// <returns>A read-only list of registered plugins.</returns>
        IReadOnlyList<IAVAPlugin> GetAllPlugins();

        /// <summary>
        /// Gets a registered plugin by its unique plugin identifier.
        /// </summary>
        /// <param name="pluginId">The unique plugin identifier.</param>
        /// <returns>The matching plugin, or <c>null</c> when no plugin is registered with that identifier.</returns>
        IAVAPlugin? GetPluginById(string pluginId);

        /// <summary>
        /// Gets all plugins that advertise a matching capability name.
        /// </summary>
        /// <param name="capabilityName">The capability name to match.</param>
        /// <returns>A read-only list of plugins that expose the requested capability.</returns>
        IReadOnlyList<IAVAPlugin> GetPluginsByCapability(string capabilityName);

        /// <summary>
        /// Registers a plugin with the runtime registry.
        /// </summary>
        /// <param name="plugin">The plugin to register.</param>
        /// <returns>A task representing the asynchronous registration operation.</returns>
        Task RegisterPluginAsync(IAVAPlugin plugin);
    }
}
