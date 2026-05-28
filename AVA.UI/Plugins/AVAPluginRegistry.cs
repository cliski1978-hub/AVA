using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Stores registered AVA plugins and exposes query helpers for runtime discovery.
    /// </summary>
    public sealed class AVAPluginRegistry : IAVAPluginRegistry
    {
        private readonly Dictionary<string, IAVAPlugin> _pluginsById =
            new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IReadOnlyList<IAVAPlugin> GetAllPlugins()
        {
            return _pluginsById.Values.ToList();
        }

        /// <inheritdoc />
        public IAVAPlugin? GetPluginById(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                return null;
            }

            _pluginsById.TryGetValue(pluginId.Trim(), out var plugin);
            return plugin;
        }

        /// <inheritdoc />
        public IReadOnlyList<IAVAPlugin> GetPluginsByCapability(string capabilityName)
        {
            if (string.IsNullOrWhiteSpace(capabilityName))
            {
                return Array.Empty<IAVAPlugin>();
            }

            var normalized = capabilityName.Trim();

            return _pluginsById.Values
                .Where(plugin => plugin.Manifest.Capabilities.Any(capability =>
                    string.Equals(capability.ToString(), normalized, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <inheritdoc />
        public Task RegisterPluginAsync(IAVAPlugin plugin)
        {
            ArgumentNullException.ThrowIfNull(plugin);

            var pluginId = plugin.Manifest.PluginId?.Trim();
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                throw new InvalidOperationException("Plugin registration requires a non-empty PluginId.");
            }

            if (_pluginsById.ContainsKey(pluginId))
            {
                throw new InvalidOperationException(
                    $"A plugin with ID '{pluginId}' has already been registered.");
            }

            _pluginsById.Add(pluginId, plugin);
            return Task.CompletedTask;
        }
    }
}
