using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Calculates live context usage metrics from an assembled prompt context package.
    /// </summary>
    public interface IContextUsageMonitor
    {
        /// <summary>
        /// Calculates deterministic context usage metrics.
        /// </summary>
        ContextUsageMetrics Calculate(
            PromptContextPackage package,
            ModelContextSettings modelSettings);
    }
}
