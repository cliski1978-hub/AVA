using System;
using AVA.UI.State;

namespace AVA.UI.Plugins
{
    /// <summary>
    /// Provides shared runtime services and application state to an initializing plugin.
    /// </summary>
    public sealed class AVAPluginContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AVAPluginContext"/> class.
        /// </summary>
        /// <param name="services">The root service provider for resolving plugin dependencies.</param>
        /// <param name="appState">The shared AVA application state.</param>
        public AVAPluginContext(IServiceProvider services, AppState appState)
        {
            Services = services;
            AppState = appState;
        }

        /// <summary>
        /// Gets the root service provider for resolving plugin dependencies.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Gets the shared AVA application state.
        /// </summary>
        public AppState AppState { get; }
    }
}
